using Granit.Core.Modularity;
using Granit.Persistence;

namespace Granit.Localization.DatabaseSource.EntityFrameworkCore;

/// <summary>
/// Granit module for EF Core persistence of localization overrides.
/// Registers <see cref="Internal.GranitLocalizationOverridesDbContext"/> and
/// <see cref="Internal.EfCoreLocalizationOverrideStore"/> wrapped by <c>CachedLocalizationOverrideStore</c>.
/// </summary>
/// <remarks>
/// Register via the host application's builder:
/// <code>
/// builder.AddGranitLocalizationEntityFrameworkCore(opt =>
///     opt.UseNpgsql(connectionString));
/// </code>
/// </remarks>
[DependsOn(
    typeof(GranitLocalizationDatabaseSourceModule),
    typeof(GranitPersistenceModule))]
public sealed class GranitLocalizationDatabaseSourceEntityFrameworkCoreModule : GranitModule;
