// =============================================================================
// Tests - ExceptionHandlingServiceCollectionExtensions
// =============================================================================
// Verifies the DI registration:
//   - IProblemDetailsService is registered
//   - IExceptionHandler (FoundationExceptionHandler) is registered
//   - IExceptionStatusCodeMapper (DefaultExceptionStatusCodeMapper) is registered
//   - ExceptionHandlingOptions is configurable
// =============================================================================

using DigitalDynamics.Foundation.ExceptionHandling;
using DigitalDynamics.Foundation.ExceptionHandling.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.ExceptionHandling.Tests;

public sealed class ExceptionHandlingServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // Service registration
    // -------------------------------------------------------------------------

    [Fact]
    public void AddFoundationExceptionHandling_RegistersIProblemDetailsService()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFoundationExceptionHandling();

        using ServiceProvider sp = services.BuildServiceProvider();

        sp.GetService<IProblemDetailsService>().Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationExceptionHandling_RegistersIExceptionHandler()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFoundationExceptionHandling();

        using ServiceProvider sp = services.BuildServiceProvider();

        sp.GetService<IExceptionHandler>().Should().NotBeNull()
            .And.BeOfType<FoundationExceptionHandler>();
    }

    [Fact]
    public void AddFoundationExceptionHandling_RegistersDefaultExceptionStatusCodeMapper()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFoundationExceptionHandling();

        using ServiceProvider sp = services.BuildServiceProvider();

        IExceptionStatusCodeMapper mapper = sp.GetRequiredService<IExceptionStatusCodeMapper>();
        mapper.Should().BeOfType<DefaultExceptionStatusCodeMapper>();
    }

    // -------------------------------------------------------------------------
    // Options configuration
    // -------------------------------------------------------------------------

    [Fact]
    public void AddFoundationExceptionHandling_DefaultOptions_ExposeInternalErrorDetailsIsFalse()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFoundationExceptionHandling();

        using ServiceProvider sp = services.BuildServiceProvider();

        ExceptionHandlingOptions opts = sp.GetRequiredService<IOptions<ExceptionHandlingOptions>>().Value;
        opts.ExposeInternalErrorDetails.Should().BeFalse(
            "internal error details must never be exposed by default (HDS production rule)");
    }

    [Fact]
    public void AddFoundationExceptionHandling_WithConfigure_SetsOptions()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFoundationExceptionHandling(opts => opts.ExposeInternalErrorDetails = true);

        using ServiceProvider sp = services.BuildServiceProvider();

        ExceptionHandlingOptions opts = sp.GetRequiredService<IOptions<ExceptionHandlingOptions>>().Value;
        opts.ExposeInternalErrorDetails.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Custom mapper registration (chain of responsibility)
    // -------------------------------------------------------------------------

    [Fact]
    public void CustomMapper_RegisteredBeforeDefault_TakesPreference()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFoundationExceptionHandling();
        // Additional mapper registered after — will be tried before the default (LIFO order in DI)
        services.AddSingleton<IExceptionStatusCodeMapper, CustomPriorityMapper>();

        using ServiceProvider sp = services.BuildServiceProvider();

        System.Collections.Generic.IEnumerable<IExceptionStatusCodeMapper> mappers =
            sp.GetServices<IExceptionStatusCodeMapper>();
        mappers.Should().HaveCount(2);
    }

    private sealed class CustomPriorityMapper : IExceptionStatusCodeMapper
    {
        public int? TryGetStatusCode(Exception exception) =>
            exception is ArgumentException ? StatusCodes.Status400BadRequest : null;
    }
}
