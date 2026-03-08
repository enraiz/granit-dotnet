namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Response containing all template variables available for a given template.
/// </summary>
/// <param name="GlobalVariables">Ambient variables injected into every template (now.*, context.*, …).</param>
/// <param name="ModelVariables">Variables from the typed data model (requires registered TemplateType).</param>
/// <param name="EnrichedVariables">Variables added by data enrichers (requires ITemplateDataEnricherMetadata).</param>
public sealed record TemplateVariablesResponse(
    IReadOnlyList<TemplateVariableItemResponse> GlobalVariables,
    IReadOnlyList<TemplateVariableItemResponse> ModelVariables,
    IReadOnlyList<TemplateVariableItemResponse> EnrichedVariables);
