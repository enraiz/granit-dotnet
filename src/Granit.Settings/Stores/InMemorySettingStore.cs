using System.Collections.Concurrent;
using Granit.Settings.Values;

namespace Granit.Settings.Stores;

/// <summary>
/// In-memory implementation of <see cref="ISettingStoreReader"/> and <see cref="ISettingStoreWriter"/>.
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread safety.
/// </summary>
public sealed class InMemorySettingStore : ISettingStoreReader, ISettingStoreWriter
{
    private readonly ConcurrentDictionary<string, SettingValue> _store = new();

    /// <inheritdoc/>
    public Task<SettingValue?> GetOrNullAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default)
    {
        _store.TryGetValue(BuildKey(name, providerName, providerKey), out SettingValue? value);
        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SettingValue>> GetListAsync(
        string providerName,
        string? providerKey,
        CancellationToken ct = default)
    {
        IReadOnlyList<SettingValue> result = [.. _store.Values.Where(v => v.ProviderName == providerName && v.ProviderKey == providerKey)];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task SetAsync(
        string name,
        string providerName,
        string? providerKey,
        string? value,
        CancellationToken ct = default)
    {
        string key = BuildKey(name, providerName, providerKey);
        SettingValue entry = new(name, providerName, providerKey, value);
        _store[key] = entry;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default)
    {
        _store.TryRemove(BuildKey(name, providerName, providerKey), out _);
        return Task.CompletedTask;
    }

    private static string BuildKey(string name, string providerName, string? providerKey) =>
        $"{providerName}:{providerKey ?? string.Empty}:{name}";
}
