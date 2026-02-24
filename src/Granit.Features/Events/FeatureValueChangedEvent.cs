namespace Granit.Features.Events;

/// <summary>
/// Raised when a feature value override is created, updated, or deleted for a tenant
/// or at the global scope.
/// </summary>
/// <remarks>
/// Consumed by <see cref="Cache.FeatureCacheInvalidationHandler"/> to remove the stale
/// entry from <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/>.
/// Publish this event after any <see cref="Store.IFeatureStore"/> mutation.
/// </remarks>
/// <param name="FeatureName">The name of the feature whose override changed.</param>
/// <param name="TenantId">
/// The tenant whose override changed, or <c>null</c> for a global (plan or default-level) change.
/// </param>
public sealed record FeatureValueChangedEvent(string FeatureName, Guid? TenantId);
