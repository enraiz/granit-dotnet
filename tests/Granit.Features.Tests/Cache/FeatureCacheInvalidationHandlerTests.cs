using Granit.Features.Cache;
using Granit.Features.Events;
using Microsoft.Extensions.Caching.Hybrid;
using Shouldly;
using Xunit;

namespace Granit.Features.Tests.Cache;

public sealed class FeatureCacheInvalidationHandlerTests
{
    // HybridCache test double that captures RemoveAsync calls
    private sealed class TrackingHybridCache : HybridCache
    {
        public List<string> RemovedKeys { get; } = [];

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key, TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            factory(state, cancellationToken);

        public override ValueTask SetAsync<T>(
            string key, T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public override ValueTask RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            RemovedKeys.Add(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_TenantEvent_RemovesCorrectCacheKey()
    {
        Guid tenantId = Guid.NewGuid();
        FeatureValueChangedEvent @event = new("App.VideoConsultation", tenantId);
        TrackingHybridCache cache = new();

        await FeatureCacheInvalidationHandler.HandleAsync(
            @event, cache, TestContext.Current.CancellationToken);

        string expectedKey = FeatureCacheKey.Build(tenantId, "App.VideoConsultation");
        cache.RemovedKeys.ShouldHaveSingleItem().ShouldBe(expectedKey);
    }

    [Fact]
    public async Task HandleAsync_GlobalEvent_RemovesGlobalCacheKey()
    {
        FeatureValueChangedEvent @event = new("App.Feature", TenantId: null);
        TrackingHybridCache cache = new();

        await FeatureCacheInvalidationHandler.HandleAsync(
            @event, cache, TestContext.Current.CancellationToken);

        string expectedKey = FeatureCacheKey.Build(null, "App.Feature");
        cache.RemovedKeys.ShouldHaveSingleItem().ShouldBe(expectedKey);
    }
}
