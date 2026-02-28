using Granit.Settings.EntityFrameworkCore.Extensions;
using Granit.Settings.EntityFrameworkCore.Internal;
using Granit.Settings.Values;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Granit.Settings.EntityFrameworkCore.Tests;

public sealed class SettingsEfCoreDiRegistrationTests
{
    private sealed class TestSettingsDbContext(DbContextOptions<TestSettingsDbContext> options)
        : DbContext(options), ISettingsDbContext
    {
        public DbSet<SettingRecord> SettingRecords => Set<SettingRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ConfigureSettingsModule();
    }

    // -------------------------------------------------------------------------
    // AddGranitSettingsEfCore
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitSettingsEfCore_RegistersISettingStore_AsSingleton()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        builder.AddGranitSettingsEfCore<TestSettingsDbContext>();

        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(ISettingStore) &&
            d.Lifetime == ServiceLifetime.Singleton,
            "EfCoreSettingStore must be registered as Singleton");
    }

    [Fact]
    public void AddGranitSettingsEfCore_ReturnsBuilder_ForChaining()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        IHostApplicationBuilder result = builder.AddGranitSettingsEfCore<TestSettingsDbContext>();

        result.ShouldBeSameAs(builder);
    }
}
