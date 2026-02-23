// =============================================================================
// Tests - WolverinePostgresqlOptions + WolverinePostgresqlOptionsValidator
// =============================================================================
// Verifies default values, section name constant, and all validation branches.
// =============================================================================

using FluentAssertions;
using Granit.Wolverine.Postgresql;
using Granit.Wolverine.Postgresql.Internal;
using Microsoft.Extensions.Options;
using Wolverine.Persistence;
using Xunit;

namespace Granit.Wolverine.Postgresql.Tests;

public sealed class WolverinePostgresqlOptionsTests
{
    // -----------------------------------------------------------------------
    // WolverinePostgresqlOptions — defaults
    // -----------------------------------------------------------------------

    [Fact]
    public void SectionName_IsWolverinePostgresql() =>
        WolverinePostgresqlOptions.SectionName.Should().Be("WolverinePostgresql");

    [Fact]
    public void DefaultTransportConnectionString_IsEmpty() =>
        new WolverinePostgresqlOptions().TransportConnectionString.Should().BeEmpty();

    [Fact]
    public void DefaultTransactionMode_IsEager() =>
        new WolverinePostgresqlOptions().TransactionMode.Should().Be(TransactionMiddlewareMode.Eager);

    // -----------------------------------------------------------------------
    // WolverinePostgresqlOptionsValidator — happy path
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_ValidConnectionString_Succeeds()
    {
        WolverinePostgresqlOptionsValidator validator = new();
        WolverinePostgresqlOptions options = new()
        {
            TransportConnectionString = "Host=localhost;Database=test;Username=user;Password=pass",
        };

        ValidateOptionsResult result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_LightweightMode_Succeeds()
    {
        WolverinePostgresqlOptionsValidator validator = new();
        WolverinePostgresqlOptions options = new()
        {
            TransportConnectionString = "Host=localhost;Database=test",
            TransactionMode = TransactionMiddlewareMode.Lightweight,
        };

        ValidateOptionsResult result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // WolverinePostgresqlOptionsValidator — TransportConnectionString failures
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_NullConnectionString_Fails()
    {
        WolverinePostgresqlOptionsValidator validator = new();
        WolverinePostgresqlOptions options = new() { TransportConnectionString = null! };

        ValidateOptionsResult result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*TransportConnectionString*");
    }

    [Fact]
    public void Validate_EmptyConnectionString_Fails()
    {
        WolverinePostgresqlOptionsValidator validator = new();
        WolverinePostgresqlOptions options = new() { TransportConnectionString = string.Empty };

        ValidateOptionsResult result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*TransportConnectionString*");
    }

    [Fact]
    public void Validate_WhitespaceConnectionString_Fails()
    {
        WolverinePostgresqlOptionsValidator validator = new();
        WolverinePostgresqlOptions options = new() { TransportConnectionString = "   " };

        ValidateOptionsResult result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*HDS*");
    }
}
