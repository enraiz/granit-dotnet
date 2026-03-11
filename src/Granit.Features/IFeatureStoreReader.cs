namespace Granit.Features;

/// <summary>
/// Reads tenant-level feature value overrides.
/// </summary>
public interface IFeatureStoreReader
{
    /// <summary>
    /// Returns the stored override for <paramref name="featureName"/> scoped to
    /// <paramref name="tenantId"/> (global scope when <c>null</c>),
    /// or <c>null</c> if no override exists.
    /// </summary>
    Task<string?> GetOrNullAsync(string featureName, string? tenantId, CancellationToken cancellationToken = default);
}
