using FluentAssertions;
using Granit.Features.EntityFrameworkCore.Extensions;
using Granit.Features.EntityFrameworkCore.Internal;
using Granit.Features.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Features.EntityFrameworkCore.Tests;

public sealed class FeaturesEfCoreDiRegistrationTests
{
    // Stub IFeatureStore to simulate a prior registration (e.g. InMemoryFeatureStore which is internal).
    private sealed class StubFeatureStore : IFeatureStore
    {
        public Task<string?> GetOrNullAsync(string featureName, string? tenantId, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task SetAsync(string featureName, string? tenantId, string value, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(string featureName, string? tenantId, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // AddGranitFeaturesEntityFrameworkCore
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitFeaturesEntityFrameworkCore_RegistersEfCoreFeatureStore()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        builder.Services.AddScoped<IFeatureStore, StubFeatureStore>(); // simulate AddGranitFeatures()

        builder.AddGranitFeaturesEntityFrameworkCore(opts =>
            opts.UseInMemoryDatabase("features-test"));

        using ServiceProvider sp = builder.Services.BuildServiceProvider();
        IFeatureStore store = sp.CreateScope().ServiceProvider.GetRequiredService<IFeatureStore>();

        store.Should().BeOfType<EfCoreFeatureStore>(
            "AddGranitFeaturesEntityFrameworkCore must replace the pre-registered store with EfCoreFeatureStore");
    }

    [Fact]
    public void AddGranitFeaturesEntityFrameworkCore_ReturnsBuilder_ForChaining()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        IHostApplicationBuilder result = builder.AddGranitFeaturesEntityFrameworkCore(opts =>
            opts.UseInMemoryDatabase("features-chain"));

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddGranitFeaturesEntityFrameworkCore_RegistersDbContextFactory_Scoped()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        builder.AddGranitFeaturesEntityFrameworkCore(opts =>
            opts.UseInMemoryDatabase("features-factory"));

        builder.Services.Should().Contain(d =>
            d.ServiceType == typeof(IDbContextFactory<GranitFeaturesDbContext>) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }
}
