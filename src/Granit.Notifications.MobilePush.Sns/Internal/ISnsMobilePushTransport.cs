using Amazon.SimpleNotificationService.Model;

namespace Granit.Notifications.MobilePush.Sns.Internal;

/// <summary>
/// Abstraction over <see cref="Amazon.SimpleNotificationService.IAmazonSimpleNotificationService"/>
/// for testability.
/// </summary>
internal interface ISnsMobilePushTransport
{
    /// <summary>Creates a platform endpoint for a device token (idempotent).</summary>
    Task<CreatePlatformEndpointResponse> CreatePlatformEndpointAsync(
        CreatePlatformEndpointRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Publishes a push notification to an SNS endpoint ARN.</summary>
    Task<PublishResponse> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default);
}
