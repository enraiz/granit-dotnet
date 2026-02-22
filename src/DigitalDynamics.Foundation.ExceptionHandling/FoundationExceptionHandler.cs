using System.Diagnostics;
using DigitalDynamics.Foundation.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.ExceptionHandling;

/// <summary>
/// Centralized exception handler for ASP.NET Core APIs.
/// Implements the native <see cref="IExceptionHandler"/> interface (.NET 8+).
/// </summary>
/// <remarks>
/// Pipeline:
/// <list type="number">
///   <item><c>OperationCanceledException</c> — logs Information, returns true without writing a response.</item>
///   <item>Resolves HTTP status code via all registered <see cref="IExceptionStatusCodeMapper"/> (chain of responsibility).</item>
///   <item>Logs at the appropriate level (5xx → Error, 4xx → Warning, 499 → Information).</item>
///   <item>Builds a <see cref="ProblemDetails"/> object (RFC 7807) with <c>traceId</c>.</item>
///   <item>Writes the response via <see cref="IProblemDetailsService.TryWriteAsync"/>.</item>
/// </list>
/// <para>
/// <b>HDS security rule:</b> messages of non-<see cref="IUserFriendlyException"/> exceptions
/// are NEVER forwarded to the client in production. The original exception is always logged.
/// </para>
/// </remarks>
internal sealed class FoundationExceptionHandler(
    IEnumerable<IExceptionStatusCodeMapper> statusCodeMappers,
    IProblemDetailsService problemDetailsService,
    IOptions<ExceptionHandlingOptions> options,
    ILoggerFactory loggerFactory,
    IStringLocalizerFactory? localizerFactory = null) : IExceptionHandler
{
    private static readonly string FallbackTitle = "An unexpected error occurred.";

    private readonly ILogger _logger = loggerFactory.CreateLogger<FoundationExceptionHandler>();

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // OperationCanceledException: client closed the connection.
        // No response is written; log at Information to avoid polluting error alerting.
        if (exception is OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled by client.");
            return true;
        }

        int statusCode = ResolveStatusCode(exception);

        LogException(exception, statusCode);

        ProblemDetails problemDetails = BuildProblemDetails(httpContext, exception, statusCode);

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private int ResolveStatusCode(Exception exception)
    {
        foreach (IExceptionStatusCodeMapper mapper in statusCodeMappers)
        {
            int? code = mapper.TryGetStatusCode(exception);
            if (code.HasValue)
            {
                return code.Value;
            }
        }

        return StatusCodes.Status500InternalServerError;
    }

    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {ExceptionType}", exception.GetType().Name);
        }
        else if (statusCode == 499)
        {
            _logger.LogInformation("Request cancelled: {ExceptionType}", exception.GetType().Name);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {ExceptionType} → {StatusCode}", exception.GetType().Name, statusCode);
        }
    }

    private ProblemDetails BuildProblemDetails(HttpContext httpContext, Exception exception, int statusCode)
    {
        string title = ResolveTitle(exception, statusCode);
        string? detail = ResolveDetail(exception, statusCode);
        string traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        problemDetails.Extensions["traceId"] = traceId;

        if (exception is IHasErrorCode hasErrorCode)
        {
            problemDetails.Extensions["errorCode"] = hasErrorCode.ErrorCode;
        }

        if (exception is IHasValidationErrors hasValidationErrors)
        {
            problemDetails.Extensions["errors"] = hasValidationErrors.ValidationErrors;
        }

        return problemDetails;
    }

    private string ResolveTitle(Exception exception, int statusCode)
    {
        // Try localized title via error code if the localizer is available.
        if (exception is IHasErrorCode hasErrorCode && localizerFactory is not null)
        {
            IStringLocalizer localizer = localizerFactory.Create(
                "Foundation",
                typeof(FoundationExceptionHandler).Assembly.GetName().Name!);
            LocalizedString localized = localizer[hasErrorCode.ErrorCode];
            if (!localized.ResourceNotFound)
            {
                return localized.Value;
            }
        }

        // For user-friendly exceptions, use the exception message as title.
        if (exception is IUserFriendlyException)
        {
            return exception.Message;
        }

        // For internal errors (5xx): mask the message in production.
        // Only expose if ExposeInternalErrorDetails is true (development/staging).
        if (statusCode >= 500)
        {
            return options.Value.ExposeInternalErrorDetails ? exception.Message : FallbackTitle;
        }

        // For 4xx without user-friendly marker: use the message (technical but not sensitive).
        return exception.Message;
    }

    private string? ResolveDetail(Exception exception, int statusCode)
    {
        // For user-friendly exceptions, detail is null (title already carries the message).
        if (exception is IUserFriendlyException)
        {
            return null;
        }

        // For internal errors: only expose stack trace in development/staging.
        if (statusCode >= 500)
        {
            return options.Value.ExposeInternalErrorDetails ? exception.ToString() : null;
        }

        return null;
    }
}
