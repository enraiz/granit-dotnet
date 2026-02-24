namespace Granit.Webhooks;

/// <summary>
/// Shared constants for the Granit.Webhooks module.
/// </summary>
internal static class WebhooksConstants
{
    /// <summary>Named HttpClient registered for webhook delivery.</summary>
    internal const string HttpClientName = "granit-webhook-delivery";

    /// <summary>Wolverine local queue for <see cref="Messages.SendWebhookCommand"/>.</summary>
    internal const string DeliveryQueueName = "webhook-delivery";

    /// <summary>Webhook envelope contract version included in every HTTP payload.</summary>
    internal const string ApiVersion = "2025-01-01";

    /// <summary>Wolverine local queue for <see cref="Messages.WebhookTrigger"/> fan-out.</summary>
    internal const string FanoutQueueName = "webhook-fanout";
}
