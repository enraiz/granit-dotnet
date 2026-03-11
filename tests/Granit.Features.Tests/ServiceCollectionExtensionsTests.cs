using Granit.Features.Definitions;
using Granit.Features.Extensions;
using Granit.Features.Internal;
using Granit.Features.ValueProviders;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Features.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddGranitFeatures
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitFeatures_Registers_IFeatureDefinitionStore_Singleton()
    {
        ServiceCollection services = new();
        services.AddGranitFeatures();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IFeatureDefinitionStore) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitFeatures_Registers_IFeatureStoreReader_Singleton()
    {
        ServiceCollection services = new();
        services.AddGranitFeatures();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IFeatureStoreReader) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitFeatures_Registers_IFeatureStoreWriter_Singleton()
    {
        ServiceCollection services = new();
        services.AddGranitFeatures();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IFeatureStoreWriter) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitFeatures_Registers_IFeatureChecker_Scoped()
    {
        ServiceCollection services = new();
        services.AddGranitFeatures();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IFeatureChecker) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitFeatures_Registers_IFeatureLimitGuard_Scoped()
    {
        ServiceCollection services = new();
        services.AddGranitFeatures();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IFeatureLimitGuard) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitFeatures_Registers_Three_IFeatureValueProviders()
    {
        ServiceCollection services = new();
        services.AddGranitFeatures();

        int count = services.Count(d => d.ServiceType == typeof(IFeatureValueProvider));
        count.ShouldBe(3, "Default, Plan, and Tenant providers must be registered");
    }

    [Fact]
    public void AddGranitFeatures_IFeatureStoreReader_IsReplaceable_WithTryAdd()
    {
        ServiceCollection services = new();
        services.AddSingleton<IFeatureStoreReader, InMemoryFeatureStore>(); // pre-register
        services.AddGranitFeatures(); // TryAdd must not replace

        services.Count(d => d.ServiceType == typeof(IFeatureStoreReader))
                .ShouldBe(1, "TryAddSingleton must not add a duplicate");
    }

    [Fact]
    public void AddGranitFeatures_IFeatureStoreWriter_IsReplaceable_WithTryAdd()
    {
        ServiceCollection services = new();
        services.AddSingleton<IFeatureStoreWriter, InMemoryFeatureStore>(); // pre-register
        services.AddGranitFeatures(); // TryAdd must not replace

        services.Count(d => d.ServiceType == typeof(IFeatureStoreWriter))
                .ShouldBe(1, "TryAddSingleton must not add a duplicate");
    }

    // -------------------------------------------------------------------------
    // AddFeatureDefinitions
    // -------------------------------------------------------------------------

    [Fact]
    public void AddFeatureDefinitions_Registers_Provider_As_Singleton()
    {
        ServiceCollection services = new();
        services.AddFeatureDefinitions<FakeProvider>();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IFeatureDefinitionProvider) &&
            d.ImplementationType == typeof(FakeProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddFeatureDefinitions_CalledTwice_RegistersBothProviders()
    {
        ServiceCollection services = new();
        services.AddFeatureDefinitions<FakeProvider>();
        services.AddFeatureDefinitions<AnotherFakeProvider>();

        services.Count(d => d.ServiceType == typeof(IFeatureDefinitionProvider))
                .ShouldBe(2);
    }

    // --- Helpers ---

    private sealed class FakeProvider : IFeatureDefinitionProvider
    {
        public void Define(IFeatureDefinitionContext context) { }
    }

    private sealed class AnotherFakeProvider : IFeatureDefinitionProvider
    {
        public void Define(IFeatureDefinitionContext context) { }
    }
}
