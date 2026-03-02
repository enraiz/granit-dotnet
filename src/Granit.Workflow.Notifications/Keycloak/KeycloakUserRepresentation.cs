using System.Text.Json.Serialization;

namespace Granit.Workflow.Notifications.Keycloak;

/// <summary>
/// Minimal DTO for deserializing Keycloak Admin API user representations.
/// Only the <see cref="Id"/> field is used for approver resolution.
/// </summary>
internal sealed record KeycloakUserRepresentation(
    [property: JsonPropertyName("id")] string Id);
