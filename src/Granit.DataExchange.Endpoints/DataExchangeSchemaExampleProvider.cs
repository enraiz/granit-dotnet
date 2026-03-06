using System.Text.Json.Nodes;
using Granit.ApiDocumentation;
using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.DataExchange.Endpoints.Dtos.Import;

namespace Granit.DataExchange.Endpoints;

/// <summary>
/// Provides OpenAPI schema examples for data exchange Request DTOs.
/// </summary>
internal sealed class DataExchangeSchemaExampleProvider : ISchemaExampleProvider
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<Type, JsonNode> GetExamples() =>
        new Dictionary<Type, JsonNode>
        {
            [typeof(ConfirmMappingsRequest)] = new JsonObject
            {
                ["mappings"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["sourceColumn"] = "Nom",
                        ["targetProperty"] = "LastName",
                        ["confidence"] = 0.95,
                    },
                },
            },
            [typeof(CreateExportJobRequest)] = new JsonObject
            {
                ["definitionName"] = "Guava.PatientExport",
                ["format"] = "xlsx",
                ["selectedFields"] = new JsonArray { "LastName", "FirstName", "Email" },
                ["includeIdForImport"] = false,
                ["sort"] = "-createdAt,lastName",
            },
            [typeof(SaveExportPresetRequest)] = new JsonObject
            {
                ["definitionName"] = "Guava.PatientExport",
                ["presetName"] = "Contact details",
                ["selectedFields"] = new JsonArray { "LastName", "FirstName", "Email", "Phone" },
                ["format"] = "xlsx",
                ["includeIdForImport"] = false,
            },
        };
}
