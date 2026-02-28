using System.Net;

namespace Granit.Notifications.Brevo.Tests;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> that captures outgoing requests
/// and returns a configurable status code.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Captured requests as (Method, Url, Body) tuples.</summary>
    public List<(string Method, string Url, string Body)> Requests { get; } = [];

    /// <summary>Status code returned by all responses. Defaults to <see cref="HttpStatusCode.OK"/>.</summary>
    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : string.Empty;

        Requests.Add((request.Method.Method, request.RequestUri?.PathAndQuery ?? "", body));

        return new HttpResponseMessage(ResponseStatusCode);
    }
}
