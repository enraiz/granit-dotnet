// =============================================================================
// Tests - FluentValidationEndpointFilter<T>
// =============================================================================
// Verifies:
//   - Valid request passes through to next delegate
//   - Invalid request returns 422 with ValidationProblemDetails
//   - Missing validator passes through (graceful degradation)
//   - Missing argument passes through
// =============================================================================

using FluentValidation;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Validation.Tests;

public sealed class FluentValidationEndpointFilterTests
{
    // -------------------------------------------------------------------------
    // Valid request → passes through
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InvokeAsync_ValidRequest_CallsNext()
    {
        TestRequest request = new("Alice", 25);
        FluentValidationEndpointFilter<TestRequest> filter = new();

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        DefaultEndpointFilterInvocationContext context =
            CreateContext(request, withValidator: true);

        await filter.InvokeAsync(context, next);

        nextCalled.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Invalid request → 422
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InvokeAsync_InvalidRequest_Returns422()
    {
        TestRequest request = new("", -1);
        FluentValidationEndpointFilter<TestRequest> filter = new();

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        DefaultEndpointFilterInvocationContext context =
            CreateContext(request, withValidator: true);

        object? result = await filter.InvokeAsync(context, next);

        nextCalled.ShouldBeFalse();
        ProblemHttpResult pr = result.ShouldBeOfType<ProblemHttpResult>();
        pr.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        HttpValidationProblemDetails vpd = pr.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
        vpd.Errors.ShouldContainKey(nameof(TestRequest.Name));
        vpd.Errors.ShouldContainKey(nameof(TestRequest.Age));
    }

    // -------------------------------------------------------------------------
    // No validator registered → passes through
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InvokeAsync_NoValidator_CallsNext()
    {
        TestRequest request = new("", -1);
        FluentValidationEndpointFilter<TestRequest> filter = new();

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        DefaultEndpointFilterInvocationContext context =
            CreateContext(request, withValidator: false);

        await filter.InvokeAsync(context, next);

        nextCalled.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // No argument of type T → passes through
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InvokeAsync_NoMatchingArgument_CallsNext()
    {
        FluentValidationEndpointFilter<TestRequest> filter = new();

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        DefaultEndpointFilterInvocationContext context =
            CreateContext(argument: "not a TestRequest", withValidator: true);

        await filter.InvokeAsync(context, next);

        nextCalled.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Multiple errors returned (CascadeMode.Continue)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InvokeAsync_MultipleErrors_ReturnsAllErrors()
    {
        TestRequest request = new("", -1);
        FluentValidationEndpointFilter<TestRequest> filter = new();

        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        DefaultEndpointFilterInvocationContext context =
            CreateContext(request, withValidator: true);

        object? result = await filter.InvokeAsync(context, next);

        ProblemHttpResult pr = result.ShouldBeOfType<ProblemHttpResult>();
        HttpValidationProblemDetails vpd = pr.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
        vpd.Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static DefaultEndpointFilterInvocationContext CreateContext(
        object argument, bool withValidator)
    {
        ServiceCollection services = new();

        if (withValidator)
        {
            services.AddSingleton<IValidator<TestRequest>, TestRequestValidator>();
        }

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider,
        };

        return new DefaultEndpointFilterInvocationContext(httpContext, argument);
    }

    private sealed record TestRequest(string Name, int Age);

    private sealed class TestRequestValidator : GranitValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Age).GreaterThan(0);
        }
    }
}
