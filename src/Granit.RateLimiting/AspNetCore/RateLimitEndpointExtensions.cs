using Granit.RateLimiting.Abstractions;
using Granit.RateLimiting.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Granit.RateLimiting.AspNetCore;

/// <summary>
/// Extension methods for applying rate limiting to ASP.NET Core endpoints.
/// </summary>
public static class RateLimitEndpointExtensions
{
    /// <summary>
    /// Applies the specified rate limiting policy to the endpoint.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyName">Name of the rate limiting policy defined in <c>RateLimiting:Policies</c>.</param>
    public static TBuilder RequireGranitRateLimiting<TBuilder>(this TBuilder builder, string policyName)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(policyName);

        return builder.AddEndpointFilter(async (context, next) =>
        {
            TenantPartitionedRateLimiter limiter = context.HttpContext.RequestServices
                .GetRequiredService<TenantPartitionedRateLimiter>();

            RateLimitResult? result = await limiter.CheckAsync(policyName, context.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            if (result is { IsAllowed: false })
            {
                context.HttpContext.Response.Headers[HeaderNames.RetryAfter] = ((int)Math.Ceiling(result.RetryAfter.TotalSeconds)).ToString();

                return TypedResults.Problem(
                    detail: $"Rate limit exceeded for policy '{policyName}'. Retry after {result.RetryAfter.TotalSeconds:F0}s.",
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Too Many Requests",
                    extensions: new Dictionary<string, object?>
                    {
                        ["policy"] = policyName,
                        ["limit"] = result.Limit,
                        ["remaining"] = result.Remaining,
                        ["retryAfter"] = (int)Math.Ceiling(result.RetryAfter.TotalSeconds),
                    });
            }

            return await next(context).ConfigureAwait(false);
        });
    }
}
