// =============================================================================
// Tests d'intégration - TenantPerDatabaseDbContextFactory — routage physique par tenant
// =============================================================================
// Valide l'isolation physique de données :
//   - Tenant A écrit dans la base A uniquement.
//   - Tenant B écrit dans la base B uniquement.
//   - Aucune fuite cross-tenant.
//
// Nécessite Docker (Testcontainers spin up deux conteneurs PostgreSQL).
// Les conteneurs sont partagés sur la classe (IClassFixture) pour amortir le coût
// de démarrage (~3 s) sur l'ensemble des tests.
// =============================================================================

using System.Net;
using Granit.Core.MultiTenancy;
using Granit.Persistence;
using Granit.Persistence.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NSubstitute;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace Granit.Wolverine.Postgresql.IntegrationTests;

// ---------------------------------------------------------------------------
// DbContext & entity used exclusively by integration tests
// ---------------------------------------------------------------------------

internal sealed class TenantRecord
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

internal sealed class TenantIntegrationDbContext(
    DbContextOptions<TenantIntegrationDbContext> options) : DbContext(options)
{
    public DbSet<TenantRecord> Records => Set<TenantRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<TenantRecord>().ToTable("tenant_records");
}

// ---------------------------------------------------------------------------
// Shared fixture — starts the two PostgreSQL containers once per test class
// ---------------------------------------------------------------------------

public sealed class TwoPostgresContainersFixture : IAsyncLifetime
{
    private PostgreSqlContainer _containerA = null!;
    private PostgreSqlContainer _containerB = null!;

    public string ConnectionStringA { get; private set; } = string.Empty;
    public string ConnectionStringB { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        _containerA = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("tenant_a")
            .WithUsername("granit")
            .WithPassword("granit_test")
            .Build();

        _containerB = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("tenant_b")
            .WithUsername("granit")
            .WithPassword("granit_test")
            .Build();

        await Task.WhenAll(_containerA.StartAsync(), _containerB.StartAsync());

        ConnectionStringA = ApplyHostOverride(_containerA.GetConnectionString());
        ConnectionStringB = ApplyHostOverride(_containerB.GetConnectionString());

        await MigrateAsync(ConnectionStringA);
        await MigrateAsync(ConnectionStringB);
    }

    public async ValueTask DisposeAsync()
    {
        await _containerA.DisposeAsync();
        await _containerB.DisposeAsync();
    }

    /// <summary>
    /// In DinD (Docker-in-Docker) CI environments, Testcontainers returns the internal
    /// bridge IP (172.17.0.x) which is not routable from the CI job container.
    /// Replace the host with TESTCONTAINERS_HOST_OVERRIDE when set, resolved to an IP
    /// address to avoid intermittent DNS failures in DinD networking.
    /// </summary>
    private static string ApplyHostOverride(string connectionString)
    {
        string? hostOverride = Environment.GetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE");
        if (string.IsNullOrEmpty(hostOverride))
        {
            return connectionString;
        }

        // Resolve hostname to IP to avoid transient DNS failures in DinD.
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(hostOverride);
            if (addresses.Length > 0)
            {
                hostOverride = addresses[0].ToString();
            }
        }
        catch
        {
            // Keep the hostname if resolution fails.
        }

        NpgsqlConnectionStringBuilder builder = new(connectionString) { Host = hostOverride };
        return builder.ConnectionString;
    }

    private static async Task MigrateAsync(string connectionString)
    {
        DbContextOptions<TenantIntegrationDbContext> opts =
            new DbContextOptionsBuilder<TenantIntegrationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        // Retry to handle transient DinD networking delays after container startup.
        const int maxAttempts = 5;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await using TenantIntegrationDbContext ctx = new(opts);
                await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(attempt * 1_000).ConfigureAwait(false);
            }
        }
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[Collection("postgres-integration")]
public sealed class PerTenantRoutingTests(TwoPostgresContainersFixture fixture)
    : IClassFixture<TwoPostgresContainersFixture>
{
    private static readonly Guid TenantAId = Guid.NewGuid();
    private static readonly Guid TenantBId = Guid.NewGuid();

    private TenantPerDatabaseDbContextFactory<TenantIntegrationDbContext> BuildFactory(
        Guid activeTenantId)
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(activeTenantId);

        ITenantConnectionStringProvider provider =
            Substitute.For<ITenantConnectionStringProvider>();
        provider
            .GetConnectionStringAsync(TenantAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(fixture.ConnectionStringA));
        provider
            .GetConnectionStringAsync(TenantBId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(fixture.ConnectionStringB));

        ServiceCollection services = new();
        IServiceProvider sp = services.BuildServiceProvider();

        TenantPerDatabaseDbContextOptions<TenantIntegrationDbContext> options = new()
        {
            Configure = static (opts, cs) => opts.UseNpgsql(cs),
        };

        return new TenantPerDatabaseDbContextFactory<TenantIntegrationDbContext>(
            currentTenant, provider, sp, options);
    }

    // -----------------------------------------------------------------------
    // Tenant A écrit dans la base A
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Write_WithTenantA_InsertsInDatabaseA()
    {
        TenantPerDatabaseDbContextFactory<TenantIntegrationDbContext> factory =
            BuildFactory(TenantAId);

        await using TenantIntegrationDbContext ctx =
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        ctx.Records.Add(new TenantRecord { Value = "record-tenant-a" });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        List<TenantRecord> rows =
            await ctx.Records.ToListAsync(TestContext.Current.CancellationToken);
        rows.ShouldContain(r => r.Value == "record-tenant-a");
    }

    // -----------------------------------------------------------------------
    // Tenant B écrit dans la base B
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Write_WithTenantB_InsertsInDatabaseB()
    {
        TenantPerDatabaseDbContextFactory<TenantIntegrationDbContext> factory =
            BuildFactory(TenantBId);

        await using TenantIntegrationDbContext ctx =
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        ctx.Records.Add(new TenantRecord { Value = "record-tenant-b" });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        List<TenantRecord> rows =
            await ctx.Records.ToListAsync(TestContext.Current.CancellationToken);
        rows.ShouldContain(r => r.Value == "record-tenant-b");
    }

    // -----------------------------------------------------------------------
    // Isolation physique — aucune fuite cross-tenant
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Write_WithTenantA_DoesNotAppearInDatabaseB()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Write via Tenant A.
        await using TenantIntegrationDbContext ctxA =
            await BuildFactory(TenantAId).CreateDbContextAsync(ct);
        ctxA.Records.Add(new TenantRecord { Value = "only-in-a" });
        await ctxA.SaveChangesAsync(ct);

        // Read via Tenant B — should not see the record.
        await using TenantIntegrationDbContext ctxB =
            await BuildFactory(TenantBId).CreateDbContextAsync(ct);
        List<TenantRecord> rowsInB = await ctxB.Records
            .Where(r => r.Value == "only-in-a")
            .ToListAsync(ct);

        rowsInB.ShouldBeEmpty("tenant isolation must prevent cross-tenant data leaks (HDS)");
    }

    [Fact]
    public async Task Write_WithTenantB_DoesNotAppearInDatabaseA()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Write via Tenant B.
        await using TenantIntegrationDbContext ctxB =
            await BuildFactory(TenantBId).CreateDbContextAsync(ct);
        ctxB.Records.Add(new TenantRecord { Value = "only-in-b" });
        await ctxB.SaveChangesAsync(ct);

        // Read via Tenant A — should not see the record.
        await using TenantIntegrationDbContext ctxA =
            await BuildFactory(TenantAId).CreateDbContextAsync(ct);
        List<TenantRecord> rowsInA = await ctxA.Records
            .Where(r => r.Value == "only-in-b")
            .ToListAsync(ct);

        rowsInA.ShouldBeEmpty("tenant isolation must prevent cross-tenant data leaks (HDS)");
    }

    // -----------------------------------------------------------------------
    // Garde — aucun tenant actif → InvalidOperationException
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateDbContextAsync_WithNoActiveTenant_ThrowsInvalidOperationException()
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns((Guid?)null);

        ITenantConnectionStringProvider provider =
            Substitute.For<ITenantConnectionStringProvider>();

        ServiceCollection services = new();
        TenantPerDatabaseDbContextOptions<TenantIntegrationDbContext> options = new()
        {
            Configure = static (opts, cs) => opts.UseNpgsql(cs),
        };

        TenantPerDatabaseDbContextFactory<TenantIntegrationDbContext> factory = new(
            currentTenant, provider, services.BuildServiceProvider(), options);

        Func<Task> act = async () =>
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        (await Should.ThrowAsync<InvalidOperationException>(act)).Message.ShouldContain("No active tenant context");
    }
}
