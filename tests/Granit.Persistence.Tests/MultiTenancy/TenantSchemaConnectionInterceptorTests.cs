// =============================================================================
// Tests - TenantSchemaConnectionInterceptor
// =============================================================================
// Vérifie que SET search_path est exécuté inconditionnellement à chaque ouverture
// de connexion, y compris sur une connexion recyclée depuis le pool Npgsql.
//
// Les connexions et commandes sont mockées via NSubstitute — aucun PostgreSQL réel requis.
// =============================================================================

using System.Data.Common;
using FluentAssertions;
using Granit.Core.MultiTenancy;
using Granit.Persistence.MultiTenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.Persistence.Tests.MultiTenancy;

public sealed class TenantSchemaConnectionInterceptorTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ICurrentTenant MakeTenant(Guid? id)
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(id);
        tenant.IsAvailable.Returns(id.HasValue);
        return tenant;
    }

    private static ITenantSchemaProvider MakeProvider(Guid tenantId, string schemaName)
    {
        ITenantSchemaProvider provider = Substitute.For<ITenantSchemaProvider>();
#pragma warning disable CA2012 // NSubstitute setup pattern
        provider.GetSchemaNameAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(schemaName));
#pragma warning restore CA2012
        return provider;
    }

    private static (DbConnection connection, DbCommand command) MakeConnection()
    {
        DbCommand cmd = Substitute.For<DbCommand>();
        cmd.ExecuteNonQueryAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

        DbConnection conn = Substitute.For<DbConnection>();
        conn.CreateCommand().Returns(cmd);

        return (conn, cmd);
    }

    private static ConnectionEndEventData MakeEventData()
    {
        FallbackEventDefinition eventDef = new(
            Substitute.For<ILoggingOptions>(),
            new EventId(0),
            LogLevel.None,
            "TestEvent",
            string.Empty);
        return new(
            eventDef,
            static (_, _) => string.Empty,
            Substitute.For<DbConnection>(),
            null,
            Guid.NewGuid(),
            false,
            DateTimeOffset.UtcNow,
            TimeSpan.Zero);
    }

    // -----------------------------------------------------------------------
    // ConnectionOpenedAsync — tenant actif → SET search_path émis
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_WhenTenantActive_ExecutesSetSearchPath()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (DbConnection conn, DbCommand cmd) = MakeConnection();
        TenantSchemaConnectionInterceptor interceptor = new(
            MakeTenant(TenantA),
            MakeProvider(TenantA, "tenant_a"));

        await interceptor.ConnectionOpenedAsync(conn, MakeEventData(), ct);

        cmd.CommandText.Should().Be("SET search_path TO tenant_a, public");
        await cmd.Received(1).ExecuteNonQueryAsync(ct);
    }

    // -----------------------------------------------------------------------
    // Sécurité pool Npgsql — connexion recyclée (SET search_path ré-exécuté)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_RecycledConnection_ReExecutesSetSearchPath()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Tenant A utilise la connexion, puis elle retourne au pool.
        (DbConnection conn, DbCommand cmd) = MakeConnection();
        TenantSchemaConnectionInterceptor interceptorA = new(
            MakeTenant(TenantA),
            MakeProvider(TenantA, "tenant_a"));
        await interceptorA.ConnectionOpenedAsync(conn, MakeEventData(), ct);

        // Tenant B récupère la même connexion physique depuis le pool.
        TenantSchemaConnectionInterceptor interceptorB = new(
            MakeTenant(TenantB),
            MakeProvider(TenantB, "tenant_b"));
        await interceptorB.ConnectionOpenedAsync(conn, MakeEventData(), ct);

        // Le SET search_path doit avoir été émis deux fois — l'intercepteur
        // ne peut pas sauter la seconde exécution même si la connexion est "déjà ouverte".
        await cmd.Received(2).ExecuteNonQueryAsync(ct);
        // Le search_path final correspond au tenant B.
        cmd.CommandText.Should().Be("SET search_path TO tenant_b, public");
    }

    // -----------------------------------------------------------------------
    // Pas de tenant → aucun SET search_path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_WhenNoTenant_DoesNotExecuteCommand()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (DbConnection conn, DbCommand cmd) = MakeConnection();
        TenantSchemaConnectionInterceptor interceptor = new(
            MakeTenant(null),
            Substitute.For<ITenantSchemaProvider>());

        await interceptor.ConnectionOpenedAsync(conn, MakeEventData(), ct);

        await cmd.DidNotReceiveWithAnyArgs().ExecuteNonQueryAsync(ct);
    }

    // -----------------------------------------------------------------------
    // ConnectionOpened (synchrone)
    // -----------------------------------------------------------------------

    [Fact]
    public void ConnectionOpened_WhenTenantActive_ExecutesSetSearchPath()
    {
        (DbConnection conn, DbCommand cmd) = MakeConnection();

        DefaultTenantSchemaProvider provider = new(
            Options.Create(new TenantSchemaOptions { Prefix = "tenant_" }));

        TenantSchemaConnectionInterceptor interceptor = new(MakeTenant(TenantA), provider);
        interceptor.ConnectionOpened(conn, MakeEventData());

        cmd.CommandText.Should().StartWith("SET search_path TO tenant_");
        cmd.Received(1).ExecuteNonQuery();
    }

    [Fact]
    public void ConnectionOpened_WhenNoTenant_DoesNotExecuteCommand()
    {
        (DbConnection conn, DbCommand cmd) = MakeConnection();
        TenantSchemaConnectionInterceptor interceptor = new(
            MakeTenant(null),
            Substitute.For<ITenantSchemaProvider>());

        interceptor.ConnectionOpened(conn, MakeEventData());

        cmd.DidNotReceiveWithAnyArgs().ExecuteNonQuery();
    }

    // -----------------------------------------------------------------------
    // Validation du nom de schéma — protection injection SQL
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("'; DROP TABLE users--")]
    [InlineData("tenant_a, public; DROP SCHEMA other--")]
    [InlineData("UPPERCASE_SCHEMA")]
    [InlineData("123startswithdigit")]
    [InlineData("")]
    public async Task ConnectionOpenedAsync_WithInvalidSchemaName_ThrowsInvalidOperationException(
        string badSchema)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (DbConnection conn, DbCommand _) = MakeConnection();
        TenantSchemaConnectionInterceptor interceptor = new(
            MakeTenant(TenantA),
            MakeProvider(TenantA, badSchema));

        Func<Task> act = () => interceptor.ConnectionOpenedAsync(conn, MakeEventData(), ct);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a valid PostgreSQL identifier*");
    }

    [Theory]
    [InlineData("'; DROP TABLE users--")]
    [InlineData("tenant_a, public; DROP SCHEMA other--")]
    [InlineData("UPPERCASE_SCHEMA")]
    [InlineData("123startswithdigit")]
    [InlineData("")]
    public void ConnectionOpened_WithInvalidSchemaName_ThrowsInvalidOperationException(
        string badSchema)
    {
        (DbConnection conn, DbCommand _) = MakeConnection();
        TenantSchemaConnectionInterceptor interceptor = new(
            MakeTenant(TenantA),
            MakeProvider(TenantA, badSchema));

        Action act = () => interceptor.ConnectionOpened(conn, MakeEventData());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a valid PostgreSQL identifier*");
    }
}
