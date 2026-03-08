namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Describes a single template variable available for use in Scriban templates.
/// </summary>
/// <param name="Name">Full variable path (e.g. "now.date", "context.culture").</param>
/// <param name="Type">CLR type name (e.g. "string", "int32", "object").</param>
/// <param name="Description">Human-readable description, if available.</param>
public sealed record TemplateVariableItemResponse(
    string Name,
    string Type,
    string? Description);
