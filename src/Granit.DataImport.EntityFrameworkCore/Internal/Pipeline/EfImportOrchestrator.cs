using System.Diagnostics;
using System.Text.Json;
using Granit.DataImport.Domain;
using Granit.DataImport.Execution;
using Granit.DataImport.Identity;
using Granit.DataImport.Mapping;
using Granit.DataImport.Parsing;
using Granit.DataImport.Pipeline;
using Granit.DataImport.Reporting;
using Granit.DataImport.Validation;
using Granit.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.DataImport.EntityFrameworkCore.Internal.Pipeline;

/// <summary>
/// EF Core implementation of <see cref="IImportOrchestrator"/>.
/// Coordinates the full pipeline: Load Job → Parse → Map → Validate → Resolve Identity → Execute.
/// </summary>
internal sealed class EfImportOrchestrator(
    IImportJobStore jobStore,
    IImportFileProvider fileProvider,
    IServiceProvider serviceProvider,
    IClock clock,
    IOptions<DataImportOptions> options) : IImportOrchestrator
{
    /// <inheritdoc/>
    public async Task<ImportReport> ExecuteAsync(Guid importJobId, CancellationToken ct = default)
    {
        ImportJob? job = await jobStore.GetAsync(importJobId, ct);
        if (job is null)
        {
            throw new InvalidOperationException($"Import job '{importJobId}' not found.");
        }

        job.Status = ImportJobStatus.Executing;
        job.ModifiedAt = clock.Now;
        await jobStore.UpdateAsync(job, ct);

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            ImportReport report = await ExecuteTypedPipelineAsync(job, ct);

            stopwatch.Stop();
            job.Status = report.FinalStatus;
            job.CompletedAt = clock.Now;
            job.ReportJson = JsonSerializer.Serialize(report);
            job.ModifiedAt = clock.Now;
            await jobStore.UpdateAsync(job, ct);

            return report;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ImportReport errorReport = new()
            {
                TotalRows = 0,
                SucceededRows = 0,
                FailedRows = 0,
                SkippedRows = 0,
                InsertedRows = 0,
                UpdatedRows = 0,
                Duration = stopwatch.Elapsed,
                FinalStatus = ImportJobStatus.Failed,
                RowErrors =
                [
                    new ImportRowError(0, ImportRowErrorKind.Persistence, ["Granit:DataImport:PipelineError"], ex.Message),
                ],
            };

            job.Status = ImportJobStatus.Failed;
            job.CompletedAt = clock.Now;
            job.ReportJson = JsonSerializer.Serialize(errorReport);
            job.ModifiedAt = clock.Now;
            await jobStore.UpdateAsync(job, ct);

            return errorReport;
        }
    }

    private async Task<ImportReport> ExecuteTypedPipelineAsync(ImportJob job, CancellationToken ct)
    {
        // Resolve the file parser for this MIME type
        IEnumerable<IFileParser> parsers = serviceProvider.GetServices<IFileParser>();
        IFileParser? parser = parsers.FirstOrDefault(p => p.CanParse(job.MimeType));
        if (parser is null)
        {
            throw new InvalidOperationException(
                $"No IFileParser registered for MIME type '{job.MimeType}'.");
        }

        // Deserialize the confirmed column mappings
        if (string.IsNullOrEmpty(job.MappingsJson))
        {
            throw new InvalidOperationException(
                $"Import job '{job.Id}' has no confirmed mappings.");
        }

        List<ColumnMapping>? mappings = JsonSerializer.Deserialize<List<ColumnMapping>>(job.MappingsJson);
        if (mappings is null || mappings.Count == 0)
        {
            throw new InvalidOperationException(
                $"Import job '{job.Id}' has invalid mappings JSON.");
        }

        // Open the file stream
        await using Stream fileStream = await fileProvider.OpenAsync(job.BlobReference, ct);
        FileParsingOptions parsingOptions = new() { MimeType = job.MimeType };

        // Build and execute the typed pipeline via reflection
        // The entity type is stored as a string — use the registered ImportDefinition to find it
        Type? executorType = FindExecutorType(job.EntityTypeName);
        if (executorType is null)
        {
            throw new InvalidOperationException(
                $"No IImportExecutor registered for entity type '{job.EntityTypeName}'.");
        }

        return await ExecuteWithReflectionAsync(
            executorType, parser, fileStream, parsingOptions, mappings, ct);
    }

    private Type? FindExecutorType(string entityTypeName)
    {
        // Scan registered IImportExecutor<T> services to find the matching entity type
        IEnumerable<ServiceDescriptor> descriptors = serviceProvider
            .GetType()
            .Assembly
            .GetTypes()
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IImportExecutor<>))
            .Select(t => t.GetGenericArguments()[0])
            .Where(t => t.Name == entityTypeName)
            .Select(t => ServiceDescriptor.Scoped(typeof(IImportExecutor<>).MakeGenericType(t), sp => sp))
            .ToList() as IEnumerable<ServiceDescriptor>;

        // Simpler approach: try to resolve the executor directly from service descriptions
        // by iterating registered services
        foreach (Type entityType in GetRegisteredEntityTypes())
        {
            if (entityType.Name == entityTypeName)
            {
                return entityType;
            }
        }

        return null;
    }

    private IEnumerable<Type> GetRegisteredEntityTypes()
    {
        // Try to resolve ImportDefinition<T> for known entity types
        // The definitions are registered as singletons — enumerate them
        IEnumerable<object> definitions = serviceProvider.GetServices<object>()
            .Where(s => s?.GetType().BaseType?.IsGenericType == true
                        && s.GetType().BaseType?.GetGenericTypeDefinition() == typeof(ImportDefinition<>));

        foreach (object definition in definitions)
        {
            Type? entityType = definition.GetType().BaseType?.GetGenericArguments()[0];
            if (entityType is not null)
            {
                yield return entityType;
            }
        }
    }

    private async Task<ImportReport> ExecuteWithReflectionAsync(
        Type entityType,
        IFileParser parser,
        Stream fileStream,
        FileParsingOptions parsingOptions,
        List<ColumnMapping> mappings,
        CancellationToken ct)
    {
        // Resolve IImportExecutor<TEntity>
        Type executorServiceType = typeof(IImportExecutor<>).MakeGenericType(entityType);
        object? executor = serviceProvider.GetService(executorServiceType);
        if (executor is null)
        {
            throw new InvalidOperationException(
                $"No IImportExecutor<{entityType.Name}> registered.");
        }

        // Resolve optional IRecordIdentityResolver<TEntity>
        Type resolverServiceType = typeof(IRecordIdentityResolver<>).MakeGenericType(entityType);
        object? identityResolver = serviceProvider.GetService(resolverServiceType);

        // Resolve optional IDataMapper<TEntity>
        Type mapperServiceType = typeof(IDataMapper<>).MakeGenericType(entityType);
        object? dataMapper = serviceProvider.GetService(mapperServiceType);

        // Resolve optional IRowValidator<TEntity>
        Type validatorServiceType = typeof(IRowValidator<>).MakeGenericType(entityType);
        object? rowValidator = serviceProvider.GetService(validatorServiceType);

        // Build the typed pipeline via a generic method
        System.Reflection.MethodInfo pipelineMethod = typeof(EfImportOrchestrator)
            .GetMethod(nameof(RunTypedPipelineAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(entityType);

        Task<ImportReport>? task = pipelineMethod.Invoke(
            this,
            [parser, fileStream, parsingOptions, mappings, executor, identityResolver, dataMapper, rowValidator, ct]) as Task<ImportReport>;

        return await task!;
    }

    private async Task<ImportReport> RunTypedPipelineAsync<TEntity>(
        IFileParser parser,
        Stream fileStream,
        FileParsingOptions parsingOptions,
        IReadOnlyList<ColumnMapping> mappings,
        IImportExecutor<TEntity> executor,
        IRecordIdentityResolver<TEntity>? identityResolver,
        IDataMapper<TEntity>? dataMapper,
        IRowValidator<TEntity>? rowValidator,
        CancellationToken ct) where TEntity : class
    {
        ImportExecutionOptions executionOptions = new()
        {
            BatchSize = options.Value.DefaultBatchSize,
        };

        // Build the streaming pipeline
        IAsyncEnumerable<ValidatedRow<TEntity>> pipeline = BuildPipeline(
            parser, fileStream, parsingOptions, mappings, identityResolver, dataMapper, rowValidator, ct);

        return await executor.ExecuteAsync(pipeline, executionOptions, null, ct);
    }

    private async IAsyncEnumerable<ValidatedRow<TEntity>> BuildPipeline<TEntity>(
        IFileParser parser,
        Stream fileStream,
        FileParsingOptions parsingOptions,
        IReadOnlyList<ColumnMapping> mappings,
        IRecordIdentityResolver<TEntity>? identityResolver,
        IDataMapper<TEntity>? dataMapper,
        IRowValidator<TEntity>? rowValidator,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct) where TEntity : class
    {
        DataImportOptions importOptions = options.Value;

        await foreach (RawImportRow row in parser.ParseAsync(fileStream, parsingOptions, ct))
        {
            // Map raw row to entity
            if (dataMapper is null)
            {
                continue;
            }

            MappingResult<TEntity> mappingResult = await dataMapper.MapAsync(row, mappings, importOptions, ct);
            if (!mappingResult.Succeeded || mappingResult.Entity is null)
            {
                continue;
            }

            TEntity entity = mappingResult.Entity;

            // Validate
            if (rowValidator is not null)
            {
                RowValidationResult validationResult = await rowValidator.ValidateAsync(entity, row.RowNumber, ct);
                if (!validationResult.IsValid)
                {
                    continue;
                }
            }

            // Resolve identity
            RecordIdentity<TEntity>? identity = null;
            if (identityResolver is not null)
            {
                identity = await identityResolver.ResolveAsync(entity, ct);
            }

            yield return new ValidatedRow<TEntity>(row.RowNumber, entity, identity);
        }
    }
}
