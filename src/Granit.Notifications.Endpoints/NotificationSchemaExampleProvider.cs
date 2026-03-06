using System.Text.Json.Nodes;
using Granit.ApiDocumentation;

namespace Granit.Notifications.Endpoints;

/// <summary>
/// Provides OpenAPI schema examples for notification Request DTOs.
/// </summary>
internal sealed class NotificationSchemaExampleProvider : ISchemaExampleProvider
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<Type, JsonNode> GetExamples() =>
        new Dictionary<Type, JsonNode>
        {
            [typeof(UpdatePreferenceRequest)] = new JsonObject
            {
                ["notificationTypeName"] = "NewMessage",
                ["channelName"] = "Email",
                ["isEnabled"] = true,
            },
        };
}
