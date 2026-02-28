// =============================================================================
// Tests - MockHttpMessageHandler
// =============================================================================
// Test double for HttpMessageHandler that captures outgoing requests and
// returns configurable responses. Used to wrap PushServiceClient without
// needing to mock its non-virtual methods.
// =============================================================================

using System.Net;

namespace Granit.Notifications.Push.Tests;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.Created;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(new HttpResponseMessage(ResponseStatusCode));
    }
}
