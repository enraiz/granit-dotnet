using FluentAssertions;
using Granit.Features.Definitions;
using Granit.Features.Store;
using Granit.Features.ValueProviders;
using Granit.Features.ValueTypes;
using Granit.Core.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Granit.Features.Tests.ValueProviders;

public sealed class TenantFeatureValueProviderTests
{
    private static FeatureDefinition MakeDefinition(string name = "App.Feature") =>
        new(name, "false", FeatureValueType.Toggle);

    private static TenantFeatureValueProvider BuildProvider(
        ICurrentTenant? currentTenant = null,
        IFeatureStore? featureStore = null)
    {
        ServiceCollection sc = new();
        if (currentTenant is not null)
        {
            sc.AddSingleton(currentTenant);
        }

        ServiceProvider sp = sc.BuildServiceProvider();
        return new TenantFeatureValueProvider(sp, featureStore ?? new InMemoryFeatureStore());
    }

    private static ICurrentTenant NoTenant()
    {
        ICurrentTenant ct = Substitute.For<ICurrentTenant>();
        ct.IsAvailable.Returns(false);
        ct.Id.Returns((Guid?)null);
        return ct;
    }

    private static ICurrentTenant WithTenant(Guid tenantId)
    {
        ICurrentTenant ct = Substitute.For<ICurrentTenant>();
        ct.IsAvailable.Returns(true);
        ct.Id.Returns(tenantId);
        return ct;
    }

    // -------------------------------------------------------------------------
    // Metadata
    // -------------------------------------------------------------------------

    [Fact]
    public void Name_Is_Tenant() =>
        BuildProvider().Name.Should().Be("Tenant");

    [Fact]
    public void Order_Is_100() =>
        BuildProvider().Order.Should().Be(100);

    // -------------------------------------------------------------------------
    // GetOrNullAsync — ICurrentTenant not registered (optional multi-tenancy)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrNullAsync_NoCurrentTenantRegistered_ReturnsNull()
    {
        TenantFeatureValueProvider provider = BuildProvider(currentTenant: null);

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.Should().BeNull("ICurrentTenant is not registered in the service provider");
    }

    // -------------------------------------------------------------------------
    // GetOrNullAsync — ICurrentTenant registered but no active tenant
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrNullAsync_NoActiveTenant_ReturnsNull()
    {
        TenantFeatureValueProvider provider = BuildProvider(currentTenant: NoTenant());

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.Should().BeNull("no tenant is active (IsAvailable = false)");
    }

    // -------------------------------------------------------------------------
    // GetOrNullAsync — tenant active, store lookup
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrNullAsync_TenantActive_NoStoreEntry_ReturnsNull()
    {
        Guid tenantId = Guid.NewGuid();
        TenantFeatureValueProvider provider = BuildProvider(
            currentTenant: WithTenant(tenantId),
            featureStore: new InMemoryFeatureStore());

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.Should().BeNull("no override stored for this tenant");
    }

    [Fact]
    public async Task GetOrNullAsync_TenantActive_StoreHasEntry_ReturnsValue()
    {
        Guid tenantId = Guid.NewGuid();
        InMemoryFeatureStore featureStore = new();
        await featureStore.SetAsync("App.Feature", tenantId.ToString(), "true",
            TestContext.Current.CancellationToken);

        TenantFeatureValueProvider provider = BuildProvider(
            currentTenant: WithTenant(tenantId),
            featureStore: featureStore);

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.Should().Be("true");
    }

    [Fact]
    public async Task GetOrNullAsync_DifferentTenant_DoesNotReturnOtherTenantValue()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        InMemoryFeatureStore featureStore = new();
        await featureStore.SetAsync("App.Feature", tenantA.ToString(), "true",
            TestContext.Current.CancellationToken);

        // Active tenant is B, but override is stored for A
        TenantFeatureValueProvider provider = BuildProvider(
            currentTenant: WithTenant(tenantB),
            featureStore: featureStore);

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.Should().BeNull("the active tenant (B) has no stored override");
    }
}
