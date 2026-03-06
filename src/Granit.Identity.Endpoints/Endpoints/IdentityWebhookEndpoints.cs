using System.Text.Json;
using Granit.Identity.Endpoints.Dtos;
using Granit.Identity.Endpoints.Internal;
using Granit.Identity.Endpoints.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Identity.Endpoints.Endpoints;

/// <summary>
/// Webhook endpoint for receiving identity provider events.
/// Validates HMAC signature and calls <see cref="IUserLookupService"/> directly.
/// </summary>
/// <remarks>
/// For asynchronous processing via Wolverine, the application host can publish
/// <c>IdentityUserUpdatedEvent</c> / <c>IdentityUserDeletedEvent</c> messages instead
/// of using this endpoint.
/// </remarks>
internal static class IdentityWebhookEndpoints
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static IEndpointRouteBuilder MapWebhookEndpoint(
        this IEndpointRouteBuilder endpoints,
        string prefix)
    {
        string webhookRoute = string.IsNullOrEmpty(prefix)
            ? "identity/webhook"
            : $"{prefix.TrimEnd('/')}/identity/webhook";

        endpoints.MapPost(webhookRoute, HandleWebhookAsync)
            .WithName("IdentityWebhook")
            .WithSummary("Receives identity provider webhook events (user created/updated/deleted).")
            .WithTags("Identity Webhook");

        return endpoints;
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, BadRequest<string>>> HandleWebhookAsync(
        HttpRequest request,
        WebhookSignatureValidator signatureValidator,
        IOptions<IdentityWebhookOptions> webhookOptions,
        IUserLookupService lookupService,
        ILogger<WebhookSignatureValidator> logger,
        CancellationToken ct)
    {
        // Read raw body for signature validation
        request.EnableBuffering();
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, ct).ConfigureAwait(false);
        byte[] body = ms.ToArray();
        request.Body.Position = 0;

        // Validate HMAC signature
        if (signatureValidator.IsEnabled)
        {
            string? signature = request.Headers[webhookOptions.Value.SignatureHeaderName].FirstOrDefault();
            if (!signatureValidator.Validate(body, signature))
            {
                return TypedResults.Unauthorized();
            }
        }

        // Parse payload
        IdentityWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<IdentityWebhookPayload>(body, s_jsonOptions);
        }
        catch (JsonException)
        {
            return TypedResults.BadRequest("Invalid JSON payload.");
        }

        if (payload is null || string.IsNullOrEmpty(payload.UserId) || string.IsNullOrEmpty(payload.EventType))
        {
            return TypedResults.BadRequest("Missing required fields: eventType, userId.");
        }

        // Process event directly via IUserLookupService
        switch (payload.EventType.ToLowerInvariant())
        {
            case "user_updated":
            case "user_created":
                await lookupService.RefreshByIdAsync(payload.UserId, ct).ConfigureAwait(false);
                break;

            case "user_deleted":
                await lookupService.DeleteByIdAsync(payload.UserId, ct).ConfigureAwait(false);
                break;

            default:
                return TypedResults.BadRequest($"Unknown event type: {payload.EventType}");
        }

        return TypedResults.Ok();
    }
}
