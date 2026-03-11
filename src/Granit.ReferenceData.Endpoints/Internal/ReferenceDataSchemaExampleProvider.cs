using System.Text.Json.Nodes;
using Granit.ApiDocumentation;
using Granit.ReferenceData.Endpoints.Dtos;

namespace Granit.ReferenceData.Endpoints.Internal;

/// <summary>
/// Provides OpenAPI schema examples for reference data Request DTOs.
/// </summary>
internal sealed class ReferenceDataSchemaExampleProvider : ISchemaExampleProvider
{
    private const string ExampleBelgique = "Belgique";
    private const string ExampleBelgica = "Bélgica";

    /// <inheritdoc/>
    public IReadOnlyDictionary<Type, JsonNode> GetExamples() =>
        new Dictionary<Type, JsonNode>
        {
            [typeof(ReferenceDataCreateRequest)] = new JsonObject
            {
                ["code"] = "BE",
                ["labelEn"] = "Belgium",
                ["labelFr"] = ExampleBelgique,
                ["labelNl"] = "België",
                ["labelDe"] = "Belgien",
                ["labelEs"] = ExampleBelgica,
                ["labelIt"] = "Belgio",
                ["labelPt"] = ExampleBelgica,
                ["sortOrder"] = 56,
            },
            [typeof(ReferenceDataUpdateRequest)] = new JsonObject
            {
                ["labelEn"] = "Belgium",
                ["labelFr"] = ExampleBelgique,
                ["labelNl"] = "België",
                ["labelDe"] = "Belgien",
                ["labelEs"] = ExampleBelgica,
                ["labelIt"] = "Belgio",
                ["labelPt"] = ExampleBelgica,
                ["sortOrder"] = 56,
                ["isActive"] = true,
            },
        };
}
