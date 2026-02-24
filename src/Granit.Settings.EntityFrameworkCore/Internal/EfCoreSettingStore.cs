using Granit.Settings.Values;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Settings.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="ISettingStore"/>.
/// Persists setting values in the host application's DbContext
/// (table <c>granit_setting_records</c>) with HDS audit trail.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a Singleton. Uses <see cref="IServiceScopeFactory"/> to create
/// a dedicated scope per database operation, ensuring thread safety and correct
/// EF Core Scoped context lifetime.
/// </para>
/// <para>
/// The host DbContext must implement <see cref="ISettingsDbContext"/> and have
/// <c>AuditedEntityInterceptor</c> wired in its factory for HDS audit trail compliance.
/// </para>
/// </remarks>
internal sealed class EfCoreSettingStore<TDbContext>(IServiceScopeFactory scopeFactory) : ISettingStore
    where TDbContext : DbContext, ISettingsDbContext
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <inheritdoc/>
    public async Task<SettingValue?> GetOrNullAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        SettingRecord? record = await context.SettingRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Name == name && r.ProviderName == providerName && r.ProviderKey == providerKey,
                ct);

        return record is null
            ? null
            : new SettingValue(record.Name, record.ProviderName, record.ProviderKey, record.Value);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SettingValue>> GetListAsync(
        string providerName,
        string? providerKey,
        CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        List<SettingRecord> records = await context.SettingRecords
            .AsNoTracking()
            .Where(r => r.ProviderName == providerName && r.ProviderKey == providerKey)
            .ToListAsync(ct);

        return records
            .Select(r => new SettingValue(r.Name, r.ProviderName, r.ProviderKey, r.Value))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string name,
        string providerName,
        string? providerKey,
        string? value,
        CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        SettingRecord? existing = await context.SettingRecords
            .FirstOrDefaultAsync(
                r => r.Name == name && r.ProviderName == providerName && r.ProviderKey == providerKey,
                ct);

        if (existing is null)
        {
            context.SettingRecords.Add(new SettingRecord
            {
                Name = name,
                ProviderName = providerName,
                ProviderKey = providerKey,
                Value = value,
            });
        }
        else
        {
            existing.Value = value;
        }

        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        SettingRecord? existing = await context.SettingRecords
            .FirstOrDefaultAsync(
                r => r.Name == name && r.ProviderName == providerName && r.ProviderKey == providerKey,
                ct);

        if (existing is null)
        {
            return;
        }

        context.SettingRecords.Remove(existing);
        await context.SaveChangesAsync(ct);
    }
}
