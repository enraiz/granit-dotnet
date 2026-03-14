using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Granit.Notifications.MobilePush.Sns.Internal;

/// <summary>
/// Production transport wrapping the AWS SNS client.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class AwsSnsMobilePushTransport(IAmazonSimpleNotificationService snsClient) : ISnsMobilePushTransport
{
    /// <inheritdoc />
    public Task<CreatePlatformEndpointResponse> CreatePlatformEndpointAsync(
        CreatePlatformEndpointRequest request,
        CancellationToken cancellationToken = default) =>
        snsClient.CreatePlatformEndpointAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<PublishResponse> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default) =>
        snsClient.PublishAsync(request, cancellationToken);
}
