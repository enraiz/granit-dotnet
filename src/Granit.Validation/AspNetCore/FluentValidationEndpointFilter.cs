using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Validation.AspNetCore;

/// <summary>
/// Minimal API endpoint filter that validates request body parameters of type <typeparamref name="T"/>
/// using a <see cref="IValidator{T}"/> resolved from the DI container.
/// </summary>
/// <typeparam name="T">The request body type to validate.</typeparam>
/// <remarks>
/// <para>
/// When validation fails, returns <c>422 Unprocessable Entity</c> with a
/// <c>HttpValidationProblemDetails</c> body containing structured error codes
/// (e.g. <c>Granit:Validation:NotEmptyValidator</c>).
/// </para>
/// <para>
/// If no <see cref="IValidator{T}"/> is registered in DI, the filter passes through
/// without validation. This allows gradual adoption: validators can be added
/// incrementally without breaking existing endpoints.
/// </para>
/// <para>
/// Register via the <see cref="ValidationEndpointConventionBuilderExtensions.ValidateBody{T}"/>
/// extension method on <see cref="RouteHandlerBuilder"/>.
/// </para>
/// </remarks>
internal sealed class FluentValidationEndpointFilter<T> : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        T? argument = default;

        foreach (object? arg in context.Arguments)
        {
            if (arg is T typed)
            {
                argument = typed;
                break;
            }
        }

        if (argument is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        IValidator<T>? validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        ValidationResult result = await validator
            .ValidateAsync(argument, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (result.IsValid)
        {
            return await next(context).ConfigureAwait(false);
        }

        // TypedResults.ValidationProblem does not support a statusCode parameter.
        // Results.ValidationProblem is needed here to enforce 422 (not 400).
#pragma warning disable GRAPI001
        return Results.ValidationProblem(
            result.ToDictionary(),
            statusCode: StatusCodes.Status422UnprocessableEntity);
#pragma warning restore GRAPI001
    }
}
