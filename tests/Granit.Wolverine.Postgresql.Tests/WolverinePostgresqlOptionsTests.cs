// =============================================================================
// Tests - WolverinePostgresqlOptions + WolverinePostgresqlOptionsValidator
// =============================================================================
// Verifies default values, section name constant, and all validation branches.
// =============================================================================

using Granit.Wolverine.Postgresql;
using Granit.Wolverine.Postgresql.Internal;
using Granit.Wolverine.Postgresql.Options;
using Microsoft.Extensions.Options;
using Shouldly;
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
        WolverinePostgresqlOptions.SectionName.ShouldBe("WolverinePostgresql");

    [Fact]
    public void DefaultTransportConnectionString_IsEmpty() =>
        new WolverinePostgresqlOptions().TransportConnectionString.ShouldBeEmpty();

    [Fact]
    public void DefaultTransactionMode_IsEager() =>
        new WolverinePostgresqlOptions().TransactionMode.ShouldBe(TransactionMiddlewareMode.Eager);

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

        result.Succeeded.ShouldBeTrue();
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

        result.Succeeded.ShouldBeTrue();
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

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(x => x.Contains("TransportConnectionString"));
    }

    [Fact]
    public void Validate_EmptyConnectionString_Fails()
    {
        WolverinePostgresqlOptionsValidator validator = new();
        WolverinePostgresqlOptions options = new() { TransportConnectionString = string.Empty };

        ValidateOptionsResult result = validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(x => x.Contains("TransportConnectionString"));
    }

    [Fact]
    public void Validate_WhitespaceConnectionString_Fails()
    {
        WolverinePostgresqlOptionsValidator validator = new();
        WolverinePostgresqlOptions options = new() { TransportConnectionString = "   " };

        ValidateOptionsResult result = validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(x => x.Contains("ISO 27001"));
    }
}
