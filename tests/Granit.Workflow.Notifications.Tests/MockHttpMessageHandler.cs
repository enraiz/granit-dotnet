using System.Net;

namespace Granit.Workflow.Notifications.Tests;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> that captures outgoing requests
/// and returns a configurable response.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Captured requests as (Method, Url, Body) tuples.</summary>
    public List<(string Method, string Url, string Body)> Requests { get; } = [];

    /// <summary>Status code returned by all responses. Defaults to <see cref="HttpStatusCode.OK"/>.</summary>
    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

    /// <summary>Response body returned by all responses. Defaults to empty.</summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : string.Empty;

        Requests.Add((request.Method.Method, request.RequestUri?.ToString() ?? "", body));

        return new HttpResponseMessage(ResponseStatusCode)
        {
            Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
