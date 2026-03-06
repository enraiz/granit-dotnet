using System.Text.Json.Nodes;
using Granit.ApiDocumentation;
using Granit.ReferenceData.Endpoints.Dtos;

namespace Granit.ReferenceData.Endpoints;

/// <summary>
/// Provides OpenAPI schema examples for reference data Request DTOs.
/// </summary>
internal sealed class ReferenceDataSchemaExampleProvider : ISchemaExampleProvider
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<Type, JsonNode> GetExamples() =>
        new Dictionary<Type, JsonNode>
        {
            [typeof(ReferenceDataCreateRequest)] = new JsonObject
            {
                ["code"] = "BE",
                ["labelEn"] = "Belgium",
                ["labelFr"] = "Belgique",
                ["labelNl"] = "België",
                ["labelDe"] = "Belgien",
                ["labelEs"] = "Bélgica",
                ["labelIt"] = "Belgio",
                ["labelPt"] = "Bélgica",
                ["sortOrder"] = 56,
            },
            [typeof(ReferenceDataUpdateRequest)] = new JsonObject
            {
                ["labelEn"] = "Belgium",
                ["labelFr"] = "Belgique",
                ["labelNl"] = "België",
                ["labelDe"] = "Belgien",
                ["labelEs"] = "Bélgica",
                ["labelIt"] = "Belgio",
                ["labelPt"] = "Bélgica",
                ["sortOrder"] = 56,
                ["isActive"] = true,
            },
        };
}
