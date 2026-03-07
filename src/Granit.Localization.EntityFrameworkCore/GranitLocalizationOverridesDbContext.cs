using Granit.Localization.EntityFrameworkCore.Internal;

using Microsoft.EntityFrameworkCore;

namespace Granit.Localization.EntityFrameworkCore;

/// <summary>
/// Dedicated EF Core DbContext for Granit localization overrides.
/// </summary>
/// <remarks>
/// <para>
/// Isolated from the host application's DbContext to avoid coupling.
/// Stores only <see cref="LocalizationOverride"/> records — the base translations
/// live in embedded JSON files resolved by <c>JsonStringLocalizer</c>.
/// </para>
/// <para>
/// Compatible with PostgreSQL (OVHcloud FR — European sovereignty, HDS compliant).
/// </para>
/// </remarks>
public sealed class GranitLocalizationOverridesDbContext(
    DbContextOptions<GranitLocalizationOverridesDbContext> options)
    : DbContext(options)
{
    /// <summary>Translation overrides indexed by resource, culture, and key.</summary>
    public DbSet<LocalizationOverride> LocalizationOverrides { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new LocalizationOverrideConfiguration());
    }
}
