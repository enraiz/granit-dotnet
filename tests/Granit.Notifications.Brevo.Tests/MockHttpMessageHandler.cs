using System.Net;
using System.Text;

namespace Granit.Notifications.Brevo.Tests;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> that captures outgoing requests
/// and returns a configurable status code and optional response body.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Captured requests as (Method, Url, Body) tuples.</summary>
    public List<(string Method, string Url, string Body)> Requests { get; } = [];

    /// <summary>Status code returned by all responses. Defaults to <see cref="HttpStatusCode.OK"/>.</summary>
    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

    /// <summary>Optional JSON response body returned on error responses.</summary>
    public string? ResponseBody { get; set; }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : string.Empty;

        Requests.Add((request.Method.Method, request.RequestUri?.PathAndQuery ?? "", body));

        var response = new HttpResponseMessage(ResponseStatusCode);
        if (ResponseBody is not null)
        {
            response.Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json");
        }

        return response;
    }
}
