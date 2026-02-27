using FluentAssertions;
using Granit.Features.Cache;
using Xunit;

namespace Granit.Features.Tests.Cache;

public sealed class FeatureCacheKeyTests
{
    [Fact]
    public void Build_WithTenantId_ReturnsTenantScopedKey()
    {
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        string key = FeatureCacheKey.Build(tenantId, "App.Video");

        key.Should().Be("features:t:11111111-1111-1111-1111-111111111111:App.Video");
    }

    [Fact]
    public void Build_WithoutTenantId_ReturnsGlobalKey()
    {
        string key = FeatureCacheKey.Build(null, "App.Video");

        key.Should().Be("features:g:App.Video");
    }

    [Fact]
    public void Build_DifferentTenants_ProduceDifferentKeys()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        string keyA = FeatureCacheKey.Build(tenantA, "App.Feature");
        string keyB = FeatureCacheKey.Build(tenantB, "App.Feature");

        keyA.Should().NotBe(keyB);
    }

    [Fact]
    public void Build_DifferentFeatures_ProduceDifferentKeys()
    {
        Guid tenantId = Guid.NewGuid();

        string key1 = FeatureCacheKey.Build(tenantId, "App.Video");
        string key2 = FeatureCacheKey.Build(tenantId, "App.Export");

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void Build_GlobalScope_DifferentFeatures_ProduceDifferentKeys()
    {
        string key1 = FeatureCacheKey.Build(null, "App.Video");
        string key2 = FeatureCacheKey.Build(null, "App.Export");

        key1.Should().NotBe(key2);
    }
}
