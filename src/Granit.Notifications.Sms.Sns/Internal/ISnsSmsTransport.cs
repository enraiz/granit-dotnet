using Amazon.SimpleNotificationService.Model;

namespace Granit.Notifications.Sms.Sns.Internal;

/// <summary>
/// Abstraction over <see cref="Amazon.SimpleNotificationService.IAmazonSimpleNotificationService"/>
/// for testability.
/// </summary>
internal interface ISnsSmsTransport
{
    /// <summary>Publishes an SMS via SNS.</summary>
    Task<PublishResponse> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default);
}
