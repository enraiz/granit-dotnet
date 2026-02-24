namespace Granit.Features.Store;

/// <summary>
/// Persistence contract for tenant-level feature value overrides.
/// </summary>
/// <remarks>
/// The default implementation is <see cref="InMemoryFeatureStore"/>.
/// Replace with <c>Granit.Features.EntityFrameworkCore</c> for a durable,
/// audited store suitable for production.
/// </remarks>
public interface IFeatureStore
{
    /// <summary>
    /// Returns the stored override for <paramref name="featureName"/> scoped to
    /// <paramref name="tenantId"/> (global scope when <c>null</c>),
    /// or <c>null</c> if no override exists.
    /// </summary>
    Task<string?> GetOrNullAsync(string featureName, string? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates the feature override for <paramref name="featureName"/>
    /// scoped to <paramref name="tenantId"/> (global scope when <c>null</c>).
    /// </summary>
    Task SetAsync(string featureName, string? tenantId, string value, CancellationToken ct = default);

    /// <summary>
    /// Deletes the feature override for <paramref name="featureName"/> scoped to
    /// <paramref name="tenantId"/> (global scope when <c>null</c>).
    /// </summary>
    Task DeleteAsync(string featureName, string? tenantId, CancellationToken ct = default);
}
