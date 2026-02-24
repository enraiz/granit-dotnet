using Granit.Core.Modularity;
using Granit.Persistence;
using Granit.Persistence.Migrations.Internal;
using Granit.Timing;
using Granit.Wolverine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Persistence.Migrations;

/// <summary>
/// Granit module for zero-downtime migrations (Expand &amp; Contract pattern).
/// </summary>
/// <remarks>
/// <para>
/// Registers provider-independent services: <see cref="IMigrationCycleRegistry"/> and
/// the default <see cref="ITenantDbIsolator"/> (no-op).
/// </para>
/// <para>
/// <see cref="MigrationProgressDbContext"/> requires a provider-specific connection string
/// and must be configured separately by calling
/// <c>AddGranitPersistenceMigrations(opts =&gt; opts.UseNpgsql(...))</c> in the application
/// startup code.
/// </para>
/// </remarks>
[DependsOn(
    typeof(GranitPersistenceModule),
    typeof(GranitTimingModule),
    typeof(GranitWolverineModule))]
public sealed class GranitPersistenceMigrationsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.TryAddSingleton<IMigrationCycleRegistry, MigrationCycleRegistry>();
        context.Services.TryAddSingleton<ITenantDbIsolator, NullTenantDbIsolator>();
        context.Services.TryAddSingleton<ITenantEnumerator, NullTenantEnumerator>();
    }
}
