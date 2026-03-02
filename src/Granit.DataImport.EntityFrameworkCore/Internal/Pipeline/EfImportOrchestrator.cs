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
            ImportReport report = await ExecuteTypedPipelineAsync(job, dryRun: false, ct);

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

    /// <inheritdoc/>
    public async Task<ImportReport> DryRunAsync(Guid importJobId, CancellationToken ct = default)
    {
        ImportJob? job = await jobStore.GetAsync(importJobId, ct);
        if (job is null)
        {
            throw new InvalidOperationException($"Import job '{importJobId}' not found.");
        }

        return await ExecuteTypedPipelineAsync(job, dryRun: true, ct);
    }

    private async Task<ImportReport> ExecuteTypedPipelineAsync(ImportJob job, bool dryRun, CancellationToken ct)
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
            executorType, parser, fileStream, parsingOptions, mappings, dryRun, ct);
    }

    private Type? FindExecutorType(string entityTypeName)
    {
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
        bool dryRun,
        CancellationToken ct)
    {
#pragma warning disable S3011 // Reflection on private member — needed to invoke generic method with runtime Type
        System.Reflection.MethodInfo pipelineMethod = typeof(EfImportOrchestrator)
            .GetMethod(nameof(RunTypedPipelineAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(entityType);
#pragma warning restore S3011

        Task<ImportReport>? task = pipelineMethod.Invoke(
            this,
            [parser, fileStream, parsingOptions, mappings, dryRun, ct]) as Task<ImportReport>;

        return await task!;
    }

    private async Task<ImportReport> RunTypedPipelineAsync<TEntity>(
        IFileParser parser,
        Stream fileStream,
        FileParsingOptions parsingOptions,
        IReadOnlyList<ColumnMapping> mappings,
        bool dryRun,
        CancellationToken ct) where TEntity : class
    {
        IImportExecutor<TEntity> executor = serviceProvider.GetRequiredService<IImportExecutor<TEntity>>();

        ImportExecutionOptions executionOptions = new()
        {
            BatchSize = options.Value.DefaultBatchSize,
            DryRun = dryRun,
        };

        // Build the streaming pipeline
        IAsyncEnumerable<ValidatedRow<TEntity>> pipeline = BuildPipeline<TEntity>(
            parser, fileStream, parsingOptions, mappings, ct);

        return await executor.ExecuteAsync(pipeline, executionOptions, null, ct);
    }

    private async IAsyncEnumerable<ValidatedRow<TEntity>> BuildPipeline<TEntity>(
        IFileParser parser,
        Stream fileStream,
        FileParsingOptions parsingOptions,
        IReadOnlyList<ColumnMapping> mappings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct) where TEntity : class
    {
        IRecordIdentityResolver<TEntity>? identityResolver = serviceProvider.GetService<IRecordIdentityResolver<TEntity>>();
        IDataMapper<TEntity>? dataMapper = serviceProvider.GetService<IDataMapper<TEntity>>();
        IRowValidator<TEntity>? rowValidator = serviceProvider.GetService<IRowValidator<TEntity>>();
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
