namespace Granit.Features.Store;

/// <summary>
/// Writes tenant-level feature value overrides.
/// </summary>
public interface IFeatureStoreWriter
{
    /// <summary>
    /// Creates or updates the feature override for <paramref name="featureName"/>
    /// scoped to <paramref name="tenantId"/> (global scope when <c>null</c>).
    /// </summary>
    Task SetAsync(string featureName, string? tenantId, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the feature override for <paramref name="featureName"/> scoped to
    /// <paramref name="tenantId"/> (global scope when <c>null</c>).
    /// </summary>
    Task DeleteAsync(string featureName, string? tenantId, CancellationToken cancellationToken = default);
}
