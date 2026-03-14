using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

namespace Granit.Notifications.Email.Ses.Internal;

/// <summary>
/// Production wrapper around <see cref="AmazonSimpleEmailServiceV2Client"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class AwsSesTransport(IAmazonSimpleEmailServiceV2 client) : ISesTransport
{
    /// <inheritdoc />
    public Task<SendEmailResponse> SendEmailAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default) =>
        client.SendEmailAsync(request, cancellationToken);

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
