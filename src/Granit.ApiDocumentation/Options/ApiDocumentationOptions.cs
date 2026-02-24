namespace Granit.ApiDocumentation.Options;

/// <summary>Configuration options for Granit OpenAPI documentation.</summary>
public sealed class ApiDocumentationOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string SectionName = "ApiDocumentation";

    /// <summary>
    /// Major API version numbers to document. Each entry generates a distinct OpenAPI document
    /// (e.g., <c>/openapi/v1.json</c>, <c>/openapi/v2.json</c>).
    /// Default: <c>[1]</c>.
    /// </summary>
    public IList<int> MajorVersions { get; set; } = [1];

    /// <summary>Title of the API shown in the Scalar UI and OpenAPI document. Default: <c>"API"</c>.</summary>
    public string Title { get; set; } = "API";

    /// <summary>
    /// Description shown in the OpenAPI document info block. Supports Markdown.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Contact email shown in the OpenAPI document info block.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// When <c>true</c>, exposes the Scalar UI and OpenAPI JSON endpoints even in Production.
    /// Default: <c>false</c> — UI is enabled in Development only.
    /// </summary>
    public bool EnableInProduction { get; set; }
}
