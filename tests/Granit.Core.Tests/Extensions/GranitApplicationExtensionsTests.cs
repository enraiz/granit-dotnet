using FluentAssertions;
using Granit.Core.Extensions;
using Granit.Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Core.Tests.Extensions;

public sealed class GranitApplicationExtensionsTests
{
    private static readonly List<string> CallOrder = [];

    public sealed class TrackingModule : GranitModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context) =>
            CallOrder.Add("sync-init");

        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            await Task.Yield();
            CallOrder.Add("async-init");
        }
    }

    public GranitApplicationExtensionsTests()
    {
        CallOrder.Clear();
    }

    // -------------------------------------------------------------------------
    // UseGranit — IHost (Worker Services)
    // -------------------------------------------------------------------------

    [Fact]
    public void UseGranit_IHost_CallsSyncInitialization()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder([]);
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<TrackingModule>();
        GranitApplication granitApp = new(modules);
        hostBuilder.Services.AddSingleton(granitApp);

        using IHost host = hostBuilder.Build();
        IHost result = host.UseGranit();

        result.Should().BeSameAs(host);
        CallOrder.Should().Contain("sync-init");
    }

    // -------------------------------------------------------------------------
    // UseGranitAsync — IHost (Worker Services)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UseGranitAsync_IHost_CallsAsyncInitialization()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder([]);
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<TrackingModule>();
        GranitApplication granitApp = new(modules);
        hostBuilder.Services.AddSingleton(granitApp);

        using IHost host = hostBuilder.Build();
        IHost result = await host.UseGranitAsync();

        result.Should().BeSameAs(host);
        CallOrder.Should().Contain("async-init");
    }
}
