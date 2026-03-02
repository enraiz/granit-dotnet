using System.Text.Json;
using Granit.DataImport.Domain;
using Granit.DataImport.Endpoints.Dtos;
using Granit.DataImport.Parsing;
using Granit.DataImport.Pipeline;
using Granit.DataImport.Reporting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataImport.Endpoints.Endpoints;

/// <summary>
/// Report and correction file endpoints (Story #498).
/// </summary>
internal static class DataImportReportEndpoints
{
    /// <summary>
    /// Registers GET /{jobId}/report, GET /{jobId}/correction-file onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{jobId:guid}/report", GetReportAsync)
            .WithName("GetImportReport")
            .WithSummary("Returns the import execution report for a completed job.");

        group.MapGet("/{jobId:guid}/correction-file", GetCorrectionFileAsync)
            .WithName("GetImportCorrectionFile")
            .WithSummary("Downloads a correction file containing only the failed rows with error annotations.");

        return group;
    }

    private static async Task<Results<Ok<ImportReportResponse>, NotFound>> GetReportAsync(
        Guid jobId,
        IImportJobStore jobStore,
        CancellationToken ct)
    {
        ImportJob? job = await jobStore.GetAsync(jobId, ct);
        if (job is null || string.IsNullOrEmpty(job.ReportJson))
        {
            return TypedResults.NotFound();
        }

        ImportReport? report = JsonSerializer.Deserialize<ImportReport>(job.ReportJson);
        if (report is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ImportReportResponse.FromReport(jobId, report));
    }

    private static async Task<Results<FileStreamHttpResult, NoContent, NotFound>> GetCorrectionFileAsync(
        Guid jobId,
        IImportJobStore jobStore,
        IImportFileProvider fileProvider,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        ImportJob? job = await jobStore.GetAsync(jobId, ct);
        if (job is null || string.IsNullOrEmpty(job.ReportJson))
        {
            return TypedResults.NotFound();
        }

        ImportReport? report = JsonSerializer.Deserialize<ImportReport>(job.ReportJson);
        if (report is null)
        {
            return TypedResults.NotFound();
        }

        if (report.RowErrors.Count == 0)
        {
            return TypedResults.NoContent();
        }

        ICorrectionFileGenerator? generator =
            serviceProvider.GetService<ICorrectionFileGenerator>();
        if (generator is null)
        {
            return TypedResults.NoContent();
        }

        Stream originalStream = await fileProvider.OpenAsync(job.BlobReference, ct);
        FileParsingOptions parsingOptions = new() { MimeType = job.MimeType };

        Stream correctionStream = await generator.GenerateAsync(
            originalStream, job.MimeType, report, parsingOptions, ct);

        string correctionFileName = $"corrections_{job.OriginalFileName}";
        return TypedResults.File(correctionStream, job.MimeType, correctionFileName);
    }
}
