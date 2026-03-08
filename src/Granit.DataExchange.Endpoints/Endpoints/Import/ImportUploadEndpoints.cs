using System.Text.Json;
using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.DataExchange.Endpoints.Dtos.Import;
using Granit.DataExchange.Endpoints.Internal.Export;
using Granit.DataExchange.Endpoints.Internal.Import;
using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Mapping;
using Granit.DataExchange.Import.Parsing;
using Granit.DataExchange.Import.Pipeline;
using Granit.Timing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataExchange.Endpoints.Endpoints.Import;

/// <summary>
/// Upload, preview, and mapping confirmation endpoints (Story #496).
/// </summary>
internal static class ImportUploadEndpoints
{
    /// <summary>
    /// Registers POST /, POST /{jobId}/preview, PUT /{jobId}/mappings onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapUploadEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", UploadAsync)
            .WithName("UploadImportFile")
            .WithSummary("Uploads a file and creates an import job.")
            .DisableAntiforgery();

        group.MapPost("/{jobId:guid}/preview", PreviewAsync)
            .WithName("PreviewImportJob")
            .WithSummary("Extracts headers, preview rows, and mapping suggestions for an import job.");

        group.MapPut("/{jobId:guid}/mappings", ConfirmMappingsAsync)
            .WithName("ConfirmImportMappings")
            .WithSummary("Confirms the column-to-property mappings for an import job.");

        return group;
    }

    private static async Task<Results<Created<ImportJobResponse>, BadRequest<string>>> UploadAsync(
        IFormFile file,
        [FromForm] string definitionName,
        IServiceProvider serviceProvider,
        IImportFileProvider fileProvider,
        IImportJobWriter jobWriter,
        IClock clock,
        CancellationToken ct)
    {
        IImportDefinitionDescriptor? descriptor =
            ImportDefinitionResolver.FindByName(serviceProvider, definitionName);
        if (descriptor is null)
        {
            return TypedResults.BadRequest($"Unknown import definition '{definitionName}'.");
        }

        if (file.Length == 0)
        {
            return TypedResults.BadRequest("File is empty.");
        }

        if (file.Length > descriptor.MaxFileSizeMb * 1024L * 1024L)
        {
            return TypedResults.BadRequest(
                $"File exceeds maximum allowed size of {descriptor.MaxFileSizeMb} MB.");
        }

        if (!descriptor.AllowedMimeTypes.Contains(file.ContentType))
        {
            return TypedResults.BadRequest(
                $"MIME type '{file.ContentType}' is not allowed. Allowed: {string.Join(", ", descriptor.AllowedMimeTypes)}.");
        }

        await using Stream stream = file.OpenReadStream();
        string blobReference = await fileProvider.SaveAsync(file.FileName, stream, ct).ConfigureAwait(false);

        ImportJob job = new()
        {
            Id = Guid.NewGuid(),
            DefinitionName = descriptor.Name,
            EntityTypeName = descriptor.EntityType.Name,
            OriginalFileName = file.FileName,
            MimeType = file.ContentType,
            FileSizeBytes = file.Length,
            BlobReference = blobReference,
            Status = ImportJobStatus.Created,
            CreatedAt = clock.Now,
        };

        await jobWriter.CreateAsync(job, ct).ConfigureAwait(false);

        return TypedResults.Created($"/{job.Id}", ImportJobResponse.FromJob(job));
    }

    private static async Task<Results<Ok<ImportPreviewResponse>, NotFound>> PreviewAsync(
        Guid jobId,
        IImportJobReader jobReader,
        IImportJobWriter jobWriter,
        IServiceProvider serviceProvider,
        IImportFileProvider fileProvider,
        IMappingSuggestionService mappingService,
        CancellationToken ct)
    {
        ImportJob? job = await jobReader.GetAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        IImportDefinitionDescriptor? descriptor =
            ImportDefinitionResolver.FindByName(serviceProvider, job.DefinitionName);
        if (descriptor is null)
        {
            return TypedResults.NotFound();
        }

        IEnumerable<IFileParser> parsers = serviceProvider.GetServices<IFileParser>();
        IFileParser? parser = parsers.FirstOrDefault(p => p.CanParse(job.MimeType));
        if (parser is null)
        {
            return TypedResults.NotFound();
        }

        FileParsingOptions parsingOptions = new() { MimeType = job.MimeType };

        await using Stream headerStream = await fileProvider.OpenAsync(job.BlobReference, ct).ConfigureAwait(false);
        IReadOnlyList<string> headers = await parser.ExtractHeadersAsync(headerStream, parsingOptions, ct).ConfigureAwait(false);

        await using Stream previewStream = await fileProvider.OpenAsync(job.BlobReference, ct).ConfigureAwait(false);
        IReadOnlyList<string[]> previewRows = await parser.ReadPreviewAsync(previewStream, parsingOptions, ct: ct).ConfigureAwait(false);

        IReadOnlyList<ImportColumnMapping> suggestions =
            await ImportDefinitionResolver.SuggestMappingsAsync(mappingService, descriptor.EntityType, headers, ct).ConfigureAwait(false);

        IReadOnlyList<ImportFieldMetadata> fieldMetadata = descriptor.GetFieldMetadata();

        job.Status = ImportJobStatus.Previewed;
        await jobWriter.UpdateAsync(job, ct).ConfigureAwait(false);

        return TypedResults.Ok(new ImportPreviewResponse(headers, previewRows, suggestions, fieldMetadata));
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> ConfirmMappingsAsync(
        Guid jobId,
        ConfirmMappingsRequest request,
        IImportJobReader jobReader,
        IImportJobWriter jobWriter,
        CancellationToken ct)
    {
        ImportJob? job = await jobReader.GetAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Mappings is null || request.Mappings.Count == 0)
        {
            return TypedResults.BadRequest("At least one column mapping is required.");
        }

        job.MappingsJson = JsonSerializer.Serialize(request.Mappings);
        job.Status = ImportJobStatus.Mapped;
        await jobWriter.UpdateAsync(job, ct).ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
