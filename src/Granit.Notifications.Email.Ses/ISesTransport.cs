using Amazon.SimpleEmailV2.Model;

namespace Granit.Notifications.Email.Ses;

/// <summary>
/// Thin abstraction over <see cref="Amazon.SimpleEmailV2.AmazonSimpleEmailServiceV2Client"/>
/// to allow unit testing without a real SES endpoint.
/// </summary>
internal interface ISesTransport : IDisposable
{
    /// <summary>Sends an email using the SES v2 API.</summary>
    Task<SendEmailResponse> SendEmailAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default);
}
