// =============================================================================
// Tests — TimelineServiceCollectionExtensions
// =============================================================================
// Verifies that AddGranitTimeline registers all expected services.
// =============================================================================

using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Security;
using Granit.Timeline.Abstractions;
using Granit.Timeline.Extensions;
using Granit.Timing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Timeline.Tests;

public sealed class TimelineServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitTimeline_RegistersTimelineStore()
    {
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        services.AddGranitTimeline();

        using ServiceProvider sp = services.BuildServiceProvider();
        ITimelineStore? store = sp.GetService<ITimelineStore>();
        store.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitTimeline_RegistersTimelineQuery()
    {
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        services.AddGranitTimeline();

        using ServiceProvider sp = services.BuildServiceProvider();
        ITimelineQuery? query = sp.GetService<ITimelineQuery>();
        query.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitTimeline_RegistersTimelineFollowerService()
    {
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        services.AddGranitTimeline();

        using ServiceProvider sp = services.BuildServiceProvider();
        ITimelineFollowerService? followerService = sp.GetService<ITimelineFollowerService>();
        followerService.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitTimeline_RegistersTimelineNotifier()
    {
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        services.AddGranitTimeline();

        using ServiceProvider sp = services.BuildServiceProvider();
        ITimelineNotifier? notifier = sp.GetService<ITimelineNotifier>();
        notifier.ShouldNotBeNull();
    }

    private static void AddRequiredDependencies(ServiceCollection services)
    {
        services.AddSingleton(Substitute.For<IClock>());
        services.AddSingleton(Substitute.For<IGuidGenerator>());
        services.AddSingleton(Substitute.For<ICurrentUserService>());
        services.AddSingleton(Substitute.For<ICurrentTenant>());
    }
}
