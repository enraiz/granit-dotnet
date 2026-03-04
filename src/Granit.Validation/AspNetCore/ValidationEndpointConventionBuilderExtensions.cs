using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Granit.Validation.AspNetCore;

/// <summary>
/// Extension methods for applying FluentValidation to Minimal API endpoints.
/// </summary>
public static class ValidationEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="FluentValidationEndpointFilter{T}"/> that validates
    /// the request body of type <typeparamref name="T"/> before the handler runs.
    /// </summary>
    /// <typeparam name="T">The request body type to validate.</typeparam>
    /// <example>
    /// <code>
    /// group.MapPost("/", HandleCreate)
    ///     .ValidateBody&lt;CreateRequest&gt;();
    /// </code>
    /// </example>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ValidateBody<T>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<FluentValidationEndpointFilter<T>>();
}
