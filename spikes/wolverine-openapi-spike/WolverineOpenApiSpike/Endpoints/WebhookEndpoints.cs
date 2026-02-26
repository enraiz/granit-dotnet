using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace WolverineOpenApiSpike.Endpoints;

/// <summary>
/// Q2 — Tests [AllowAnonymousTenant] custom attribute propagation.
/// Q4 — Tests if Wolverine endpoints can be assigned to a specific OpenAPI document group.
/// </summary>
public static class WebhookEndpoints
{
    // Public endpoint — no [Authorize], has [AllowAnonymousTenant]
    [WolverinePost("/api/webhooks/sih")]
    [Tags("Webhooks")]
    [AllowAnonymous]
    [AllowAnonymousTenant]
    public static IResult ReceiveSihWebhook(SihWebhookPayload payload)
    {
        return Results.Accepted();
    }
}

public sealed record SihWebhookPayload(
    string EventType,
    string ResourceId,
    DateTime Timestamp);
