// =============================================================================
// Tests - MockHttpMessageHandler
// =============================================================================
// Test doubles for HttpMessageHandler that capture outgoing requests and
// return configurable responses. Used to wrap PushServiceClient without
// needing to mock its non-virtual methods.
// =============================================================================

using System.Net;

namespace Granit.Notifications.Push.Tests;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public List<byte[]> RequestBodies { get; } = [];

    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.Created;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (request.Content is not null)
        {
            byte[] body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            RequestBodies.Add(body);
        }
        else
        {
            RequestBodies.Add([]);
        }

        return new HttpResponseMessage(ResponseStatusCode);
    }
}

/// <summary>
/// Mock handler that returns a different <see cref="HttpStatusCode"/> for each
/// successive request, following the order provided at construction time.
/// </summary>
internal sealed class SequentialMockHttpMessageHandler : HttpMessageHandler
{
    private readonly IReadOnlyList<HttpStatusCode> _statusCodes;
    private int _requestIndex;

    public List<HttpRequestMessage> Requests { get; } = [];

    public SequentialMockHttpMessageHandler(IReadOnlyList<HttpStatusCode> statusCodes)
    {
        _statusCodes = statusCodes;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        HttpStatusCode statusCode = _requestIndex < _statusCodes.Count
            ? _statusCodes[_requestIndex]
            : HttpStatusCode.Created;
        _requestIndex++;
        return Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
