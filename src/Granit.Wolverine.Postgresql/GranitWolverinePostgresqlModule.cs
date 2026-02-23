using Granit.Core.Modularity;
using Granit.Persistence;
using Granit.Wolverine.Postgresql.Extensions;
using Microsoft.Extensions.Configuration;

namespace Granit.Wolverine.Postgresql;

/// <summary>
/// Granit module for PostgreSQL-backed durable Wolverine messaging.
/// </summary>
/// <remarks>
/// Adds a PostgreSQL Outbox and EF Core transaction integration on top of the
/// provider-agnostic core configured by <see cref="GranitWolverineModule"/>.
/// <para>
/// Reads the PostgreSQL connection string from
/// <c>ConnectionStrings:Wolverine</c> in <c>appsettings.json</c>.
/// </para>
/// <para>
/// For EF Core transaction integration, the consuming service must register its
/// <c>DbContext</c> via <c>services.AddDbContextWithWolverineIntegration&lt;TContext&gt;()</c>
/// instead of the standard <c>services.AddDbContext&lt;TContext&gt;()</c>.
/// </para>
/// </remarks>
[DependsOn(typeof(GranitWolverineModule), typeof(GranitPersistenceModule))]
public sealed class GranitWolverinePostgresqlModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        string connectionString = context.Configuration.GetConnectionString("Wolverine")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:Wolverine' in configuration. " +
                "This connection string is required by GranitWolverinePostgresqlModule " +
                "to configure the PostgreSQL Outbox.");

        context.Builder.AddGranitWolverineWithPostgresql(connectionString);
    }
}
