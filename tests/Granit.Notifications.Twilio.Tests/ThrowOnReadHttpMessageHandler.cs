using System.Net;

namespace Granit.Notifications.Twilio.Tests;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> that returns a non-2xx response
/// whose content throws on <see cref="HttpContent.ReadAsStringAsync(CancellationToken)"/>,
/// exercising the catch block in <c>EnsureSuccessAsync</c>.
/// </summary>
internal sealed class ThrowOnReadHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Consume the request content normally so PostAsync completes.
        if (request.Content is not null)
        {
            await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(statusCode)
        {
            Content = new ThrowingContent(),
        };
    }

    private sealed class ThrowingContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new IOException("Simulated read failure");

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
