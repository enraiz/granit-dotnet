using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Granit.Notifications.Sms.Sns.Internal;

/// <summary>
/// Production transport wrapping the AWS SNS client.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class AwsSnsSmsTransport(IAmazonSimpleNotificationService snsClient) : ISnsSmsTransport
{
    /// <inheritdoc />
    public Task<PublishResponse> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default) =>
        snsClient.PublishAsync(request, cancellationToken);
}
