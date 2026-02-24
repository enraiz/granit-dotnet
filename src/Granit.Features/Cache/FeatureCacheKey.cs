namespace Granit.Features.Cache;

/// <summary>
/// Builds <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> keys
/// for resolved feature values.
/// </summary>
internal static class FeatureCacheKey
{
    internal static string Build(Guid? tenantId, string featureName) =>
        tenantId.HasValue
            ? $"features:t:{tenantId.Value}:{featureName}"
            : $"features:g:{featureName}";
}
