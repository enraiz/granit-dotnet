using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Granit.ApiDocumentation.Transformers;

/// <summary>
/// Adds RFC 7807 ProblemDetails error responses to OpenAPI operations based on endpoint metadata.
/// <list type="bullet">
///   <item>401 + 403: added when <c>[Authorize]</c> is present (unless <c>[AllowAnonymous]</c>)</item>
///   <item>422: added when the operation has a request body (validation errors)</item>
///   <item>500: added on all operations</item>
/// </list>
/// Also removes phantom 404 responses added by Wolverine on endpoints without route parameters.
/// </summary>
internal sealed class ProblemDetailsResponseOperationTransformer : IOpenApiOperationTransformer
{
    private const string ProblemDetailsMediaType = "application/problem+json";

    /// <inheritdoc/>
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        IList<object> metadata = context.Description.ActionDescriptor.EndpointMetadata;
        bool hasAuthorize = metadata.OfType<AuthorizeAttribute>().Any();
        bool hasAllowAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();
        bool isProtected = hasAuthorize && !hasAllowAnonymous;
        bool hasRequestBody = operation.RequestBody is not null;
        bool hasRouteParameter = operation.Parameters?
            .Any(p => p.In == ParameterLocation.Path) ?? false;

        operation.Responses ??= new();

        // Remove phantom 404 from Wolverine on endpoints without route parameters.
        if (!hasRouteParameter && operation.Responses.ContainsKey("404"))
        {
            operation.Responses.Remove("404");
        }

        OpenApiResponses responses = operation.Responses;

        if (isProtected)
        {
            EnsureResponse(responses, "401", "Unauthorized");
            EnsureResponse(responses, "403", "Forbidden");
        }

        if (hasRequestBody)
        {
            EnsureResponse(responses, "422", "Unprocessable Entity");
        }

        EnsureResponse(responses, "500", "Internal Server Error");

        return Task.CompletedTask;
    }

    private static void EnsureResponse(
        OpenApiResponses responses,
        string statusCode,
        string description)
    {
        if (responses.ContainsKey(statusCode))
        {
            return;
        }

        responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [ProblemDetailsMediaType] = new OpenApiMediaType(),
            },
        };
    }
}
