// =============================================================================
// Tests - EfCoreExceptionStatusCodeMapper
// =============================================================================
// Verifies:
//   - DbUpdateConcurrencyException → 409 Conflict
//   - Other exceptions → null (delegation to next mapper)
//   - Conditional registration: mapper is registered when ExceptionHandling is configured
//   - No registration when ExceptionHandling is not configured
// =============================================================================

using DigitalDynamics.Foundation.ExceptionHandling;
using DigitalDynamics.Foundation.ExceptionHandling.Extensions;
using DigitalDynamics.Foundation.Persistence.ExceptionHandling;
using DigitalDynamics.Foundation.Persistence.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalDynamics.Foundation.Persistence.Tests;

public sealed class EfCoreExceptionStatusCodeMapperTests
{
    // -------------------------------------------------------------------------
    // Unit tests: mapper logic
    // -------------------------------------------------------------------------

    [Fact]
    public void DbUpdateConcurrencyException_Returns409()
    {
        EfCoreExceptionStatusCodeMapper mapper = new();

        int? result = mapper.TryGetStatusCode(new DbUpdateConcurrencyException());

        result.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void InvalidOperationException_ReturnsNull()
    {
        EfCoreExceptionStatusCodeMapper mapper = new();

        int? result = mapper.TryGetStatusCode(new InvalidOperationException("unrelated"));

        result.Should().BeNull("EfCoreMapper must delegate unknown exceptions to the next mapper");
    }

    [Fact]
    public void ArgumentException_ReturnsNull()
    {
        EfCoreExceptionStatusCodeMapper mapper = new();

        int? result = mapper.TryGetStatusCode(new ArgumentException("unrelated"));

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Integration: conditional DI registration
    // -------------------------------------------------------------------------

    [Fact]
    public void AddFoundationPersistence_WithExceptionHandling_RegistersEfCoreMapper()
    {
        ServiceCollection services = new();
        services.AddLogging();

        // ExceptionHandling registered first
        services.AddFoundationExceptionHandling();
        services.AddFoundationPersistence();

        using ServiceProvider sp = services.BuildServiceProvider();

        System.Collections.Generic.IEnumerable<IExceptionStatusCodeMapper> mappers =
            sp.GetServices<IExceptionStatusCodeMapper>();

        mappers.Should().Contain(m => m is EfCoreExceptionStatusCodeMapper,
            "EfCoreExceptionStatusCodeMapper must be registered when ExceptionHandling is active");
    }

    [Fact]
    public void AddFoundationPersistence_WithoutExceptionHandling_DoesNotRegisterEfCoreMapper()
    {
        ServiceCollection services = new();
        services.AddLogging();

        // ExceptionHandling NOT registered
        services.AddFoundationPersistence();

        using ServiceProvider sp = services.BuildServiceProvider();

        System.Collections.Generic.IEnumerable<IExceptionStatusCodeMapper> mappers =
            sp.GetServices<IExceptionStatusCodeMapper>();

        mappers.Should().NotContain(m => m is EfCoreExceptionStatusCodeMapper,
            "EfCoreExceptionStatusCodeMapper must not be registered without ExceptionHandling");
    }
}
