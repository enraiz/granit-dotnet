// =============================================================================
// Tests - FoundationExceptionHandler
// =============================================================================
// Verifies the full handler pipeline:
//   - OperationCanceledException: returns true, no response written
//   - EntityNotFoundException → 404, traceId in extensions
//   - BusinessException → 400, errorCode in extensions
//   - ValidationException → 422, errors in extensions
//   - HDS security: 5xx message masked in production (ExposeInternalErrorDetails = false)
//   - HDS security: 5xx message exposed in development (ExposeInternalErrorDetails = true)
//   - traceId always present in extensions
// =============================================================================

using System.Diagnostics;
using DigitalDynamics.Foundation.Core.Exceptions;
using DigitalDynamics.Foundation.ExceptionHandling;
using DigitalDynamics.Foundation.ExceptionHandling.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.ExceptionHandling.Tests;

public sealed class FoundationExceptionHandlerTests
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // -------------------------------------------------------------------------
    // Test helpers
    // -------------------------------------------------------------------------

    private static ServiceProvider BuildServiceProvider(
        Action<ExceptionHandlingOptions>? configureOptions = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFoundationExceptionHandling(configureOptions);
        return services.BuildServiceProvider();
    }

    private static async Task<(int StatusCode, IDictionary<string, object?> Extensions, string? Title, string? Detail)>
        InvokeHandlerAsync(
            ServiceProvider sp,
            Exception exception)
    {
        FoundationExceptionHandler handler = (FoundationExceptionHandler)sp
            .GetRequiredService<Microsoft.AspNetCore.Diagnostics.IExceptionHandler>();

        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new System.IO.MemoryStream();

        bool handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        handled.Should().BeTrue();

        // Read ProblemDetails written to the body
        httpContext.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        ProblemDetails? problem = await System.Text.Json.JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            JsonOptions);

        return (
            problem?.Status ?? 0,
            problem?.Extensions ?? new Dictionary<string, object?>(),
            problem?.Title,
            problem?.Detail);
    }

    // -------------------------------------------------------------------------
    // OperationCanceledException: no response, return true
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OperationCanceled_ReturnsTrueWithoutWritingResponse()
    {
        using ServiceProvider sp = BuildServiceProvider();
        FoundationExceptionHandler handler = (FoundationExceptionHandler)sp
            .GetRequiredService<Microsoft.AspNetCore.Diagnostics.IExceptionHandler>();

        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new System.IO.MemoryStream();

        bool handled = await handler.TryHandleAsync(
            httpContext, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.Body.Length.Should().Be(0, "no response should be written for cancelled requests");
    }

    // -------------------------------------------------------------------------
    // Status code mapping through handler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EntityNotFoundException_ReturnsStatus404()
    {
        using ServiceProvider sp = BuildServiceProvider();

        (int statusCode, _, _, _) = await InvokeHandlerAsync(
            sp, new EntityNotFoundException(typeof(object), 1));

        statusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task BusinessException_ReturnsStatus400()
    {
        using ServiceProvider sp = BuildServiceProvider();

        (int statusCode, _, _, _) = await InvokeHandlerAsync(
            sp, new BusinessException("Test:Error", "Business rule violated."));

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ValidationException_ReturnsStatus422()
    {
        using ServiceProvider sp = BuildServiceProvider();
        Dictionary<string, string[]> errors = new() { ["Email"] = ["Required"] };

        (int statusCode, _, _, _) = await InvokeHandlerAsync(
            sp, new Core.Exceptions.ValidationException(errors));

        statusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // Extensions: traceId always present
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnyException_TraceIdPresentInExtensions()
    {
        using ServiceProvider sp = BuildServiceProvider();

        (_, IDictionary<string, object?> extensions, _, _) = await InvokeHandlerAsync(
            sp, new BusinessException("Test:Error"));

        extensions.Should().ContainKey("traceId");
        extensions["traceId"].Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // Extensions: errorCode for IHasErrorCode
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BusinessException_ErrorCodeInExtensions()
    {
        using ServiceProvider sp = BuildServiceProvider();

        (_, IDictionary<string, object?> extensions, _, _) = await InvokeHandlerAsync(
            sp, new BusinessException("Appointment:SlotUnavailable", "Slot unavailable."));

        extensions.Should().ContainKey("errorCode");
    }

    [Fact]
    public async Task EntityNotFoundException_NoErrorCodeInExtensions()
    {
        using ServiceProvider sp = BuildServiceProvider();

        (_, IDictionary<string, object?> extensions, _, _) = await InvokeHandlerAsync(
            sp, new EntityNotFoundException(typeof(object), 99));

        extensions.Should().NotContainKey("errorCode");
    }

    // -------------------------------------------------------------------------
    // HDS security: 5xx masking in production
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InternalException_Production_TitleIsMasked()
    {
        // ExposeInternalErrorDetails = false (default = production behaviour)
        using ServiceProvider sp = BuildServiceProvider(opts => opts.ExposeInternalErrorDetails = false);

        (_, _, string? title, _) = await InvokeHandlerAsync(
            sp, new InvalidOperationException("Patient#12345 caused NullRef"));

        title.Should().Be("An unexpected error occurred.",
            "internal exception messages must never be exposed in production");
        title.Should().NotContain("Patient");
    }

    [Fact]
    public async Task InternalException_Development_TitleExposesMessage()
    {
        // ExposeInternalErrorDetails = true (development/staging)
        using ServiceProvider sp = BuildServiceProvider(opts => opts.ExposeInternalErrorDetails = true);

        (_, _, string? title, _) = await InvokeHandlerAsync(
            sp, new InvalidOperationException("Detailed dev error"));

        title.Should().Be("Detailed dev error");
    }

    // -------------------------------------------------------------------------
    // UserFriendlyException: title comes from exception message
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BusinessException_TitleEqualsMessage()
    {
        using ServiceProvider sp = BuildServiceProvider();

        (_, _, string? title, _) = await InvokeHandlerAsync(
            sp, new BusinessException("Test:Error", "The business rule was violated."));

        title.Should().Be("The business rule was violated.");
    }
}
