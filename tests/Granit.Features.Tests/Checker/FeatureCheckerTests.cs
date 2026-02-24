using FluentAssertions;
using Granit.Features.Checker;
using Granit.Features.Definitions;
using Granit.Features.Exceptions;
using Granit.Features.Store;
using Granit.Features.ValueProviders;
using Granit.Features.ValueTypes;
using Granit.MultiTenancy;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Xunit;

namespace Granit.Features.Tests.Checker;

public sealed class FeatureCheckerTests
{
    // HybridCache test double: always calls the factory (no caching).
    private sealed class NoopHybridCache : HybridCache
    {
        public override async ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            await factory(state, cancellationToken);

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public override ValueTask RemoveAsync(
            string key,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public override ValueTask RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
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

    private static FeatureChecker BuildChecker(
        IFeatureDefinitionStore store,
        ICurrentTenant currentTenant,
        params IFeatureValueProvider[] providers) =>
        new(store, providers, currentTenant, new NoopHybridCache());

    // -------------------------------------------------------------------------
    // IsEnabledAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task IsEnabledAsync_DefaultTrue_Returns_True()
    {
        IFeatureDefinitionStore store = Substitute.For<IFeatureDefinitionStore>();
        store.GetRequired("App.Feature").Returns(new FeatureDefinition("App.Feature", "true", FeatureValueType.Toggle));
        FeatureChecker checker = BuildChecker(store, NoTenant(),
            new DefaultValueFeatureValueProvider());

        bool result = await checker.IsEnabledAsync("App.Feature", TestContext.Current.CancellationToken);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_DefaultFalse_Returns_False()
    {
        IFeatureDefinitionStore store = Substitute.For<IFeatureDefinitionStore>();
        store.GetRequired("App.Feature").Returns(new FeatureDefinition("App.Feature", "false", FeatureValueType.Toggle));
        FeatureChecker checker = BuildChecker(store, NoTenant(),
            new DefaultValueFeatureValueProvider());

        bool result = await checker.IsEnabledAsync("App.Feature", TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // GetNumericAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetNumericAsync_ReturnsDefaultNumericValue()
    {
        IFeatureDefinitionStore store = Substitute.For<IFeatureDefinitionStore>();
        store.GetRequired("App.MaxPatients").Returns(new FeatureDefinition("App.MaxPatients", "50", FeatureValueType.Numeric));
        FeatureChecker checker = BuildChecker(store, NoTenant(),
            new DefaultValueFeatureValueProvider());

        long result = await checker.GetNumericAsync("App.MaxPatients", TestContext.Current.CancellationToken);

        result.Should().Be(50);
    }

    // -------------------------------------------------------------------------
    // RequireEnabledAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RequireEnabledAsync_FeatureEnabled_DoesNotThrow()
    {
        IFeatureDefinitionStore store = Substitute.For<IFeatureDefinitionStore>();
        store.GetRequired("App.Feature").Returns(new FeatureDefinition("App.Feature", "true", FeatureValueType.Toggle));
        FeatureChecker checker = BuildChecker(store, NoTenant(),
            new DefaultValueFeatureValueProvider());

        Func<Task> act = () => checker.RequireEnabledAsync("App.Feature", TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequireEnabledAsync_FeatureDisabled_Throws_FeatureNotEnabledException()
    {
        IFeatureDefinitionStore store = Substitute.For<IFeatureDefinitionStore>();
        store.GetRequired("App.Feature").Returns(new FeatureDefinition("App.Feature", "false", FeatureValueType.Toggle));
        FeatureChecker checker = BuildChecker(store, NoTenant(),
            new DefaultValueFeatureValueProvider());

        Func<Task> act = () => checker.RequireEnabledAsync("App.Feature", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FeatureNotEnabledException>()
            .WithMessage("*App.Feature*");
    }

    // -------------------------------------------------------------------------
    // Cascade: Tenant overrides Default
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TenantOverride_Takes_Precedence_Over_Default()
    {
        Guid tenantId = Guid.NewGuid();
        InMemoryFeatureStore featureStore = new();
        await featureStore.SetAsync("App.VideoConsultation", tenantId.ToString(), "true",
            TestContext.Current.CancellationToken);

        IFeatureDefinitionStore store = Substitute.For<IFeatureDefinitionStore>();
        store.GetRequired("App.VideoConsultation")
            .Returns(new FeatureDefinition("App.VideoConsultation", "false", FeatureValueType.Toggle));

        ICurrentTenant currentTenant = WithTenant(tenantId);
        TenantFeatureValueProvider tenantProvider = new(currentTenant, featureStore);

        FeatureChecker checker = BuildChecker(store, currentTenant,
            tenantProvider,
            new DefaultValueFeatureValueProvider());

        bool result = await checker.IsEnabledAsync("App.VideoConsultation", TestContext.Current.CancellationToken);

        result.Should().BeTrue("tenant override is 'true' even though default is 'false'");
    }

    [Fact]
    public async Task NoTenantContext_Falls_Back_To_Default()
    {
        InMemoryFeatureStore featureStore = new();
        IFeatureDefinitionStore store = Substitute.For<IFeatureDefinitionStore>();
        store.GetRequired("App.VideoConsultation")
            .Returns(new FeatureDefinition("App.VideoConsultation", "false", FeatureValueType.Toggle));

        TenantFeatureValueProvider tenantProvider = new(NoTenant(), featureStore);

        FeatureChecker checker = BuildChecker(store, NoTenant(),
            tenantProvider,
            new DefaultValueFeatureValueProvider());

        bool result = await checker.IsEnabledAsync("App.VideoConsultation", TestContext.Current.CancellationToken);

        result.Should().BeFalse("no tenant context, falls back to default 'false'");
    }
}
