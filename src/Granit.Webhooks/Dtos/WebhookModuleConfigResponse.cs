namespace Granit.Webhooks.Dtos;

/// <summary>
/// Response exposing the current webhook module configuration.
/// </summary>
public sealed record WebhookModuleConfigResponse(bool StorePayload);
