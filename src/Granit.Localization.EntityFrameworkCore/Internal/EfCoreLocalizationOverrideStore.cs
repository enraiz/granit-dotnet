using Microsoft.EntityFrameworkCore;

namespace Granit.Localization.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="ILocalizationOverrideStore"/>.
/// Persists translation overrides in PostgreSQL (table <c>i18n_localization_overrides</c>)
/// with HDS audit trail.
/// </summary>
/// <remarks>
/// Registered as a keyed service (<c>"efcore-raw"</c>) and wrapped by
/// <c>CachedLocalizationOverrideStore</c> which is the default <see cref="ILocalizationOverrideStore"/>.
/// Each operation uses <see cref="IDbContextFactory{TContext}"/> to create and dispose its own
/// <see cref="GranitLocalizationOverridesDbContext"/>, making it safe for concurrent request handling.
/// </remarks>
internal sealed class EfCoreLocalizationOverrideStore(
    IDbContextFactory<GranitLocalizationOverridesDbContext> contextFactory) : ILocalizationOverrideStore
{
    private readonly IDbContextFactory<GranitLocalizationOverridesDbContext> _contextFactory = contextFactory;

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, string>> GetOverridesAsync(
        string resourceName, string culture, CancellationToken ct = default)
    {
        await using GranitLocalizationOverridesDbContext context =
            await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<LocalizationOverride> rows = await context.LocalizationOverrides
            .AsNoTracking()
            .Where(o => o.ResourceName == resourceName && o.CultureName == culture)
            .ToListAsync(ct).ConfigureAwait(false);

        return rows.ToDictionary(o => o.Key, o => o.Value, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public async Task SetOverrideAsync(
        string resourceName, string culture, string key, string value, CancellationToken ct = default)
    {
        await using GranitLocalizationOverridesDbContext context =
            await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        LocalizationOverride? existing = await context.LocalizationOverrides
            .FirstOrDefaultAsync(
                o => o.ResourceName == resourceName && o.CultureName == culture && o.Key == key,
                ct).ConfigureAwait(false);

        if (existing is null)
        {
            context.LocalizationOverrides.Add(new LocalizationOverride
            {
                ResourceName = resourceName,
                CultureName = culture,
                Key = key,
                Value = value,
            });
        }
        else
        {
            existing.Value = value;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveOverrideAsync(
        string resourceName, string culture, string key, CancellationToken ct = default)
    {
        await using GranitLocalizationOverridesDbContext context =
            await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        LocalizationOverride? existing = await context.LocalizationOverrides
            .FirstOrDefaultAsync(
                o => o.ResourceName == resourceName && o.CultureName == culture && o.Key == key,
                ct).ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        context.LocalizationOverrides.Remove(existing);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
