using Shouldly;
using Granit.Features.Definitions;
using Granit.Features.Plans;
using Granit.Features.ValueProviders;
using Granit.Features.ValueTypes;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Granit.Features.Tests.ValueProviders;

public sealed class PlanFeatureValueProviderTests
{
    private static FeatureDefinition MakeDefinition(string name = "App.Feature") =>
        new(name, "false", FeatureValueType.Toggle);

    private static PlanFeatureValueProvider BuildProvider(
        IPlanIdProvider? planIdProvider = null,
        IPlanFeatureStore? planFeatureStore = null)
    {
        ServiceCollection services = new();
        if (planIdProvider is not null)
        {
            services.AddSingleton(planIdProvider);
        }

        if (planFeatureStore is not null)
        {
            services.AddSingleton(planFeatureStore);
        }

        ServiceProvider sp = services.BuildServiceProvider();
        return new PlanFeatureValueProvider(sp);
    }

    // -------------------------------------------------------------------------
    // Metadata
    // -------------------------------------------------------------------------

    [Fact]
    public void Name_Is_Plan() =>
        BuildProvider().Name.ShouldBe("Plan");

    [Fact]
    public void Order_Is_200() =>
        BuildProvider().Order.ShouldBe(200);

    // -------------------------------------------------------------------------
    // GetOrNullAsync — provider not registered
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrNullAsync_NoPlanIdProvider_ReturnsNull()
    {
        PlanFeatureValueProvider provider = BuildProvider(planFeatureStore: Substitute.For<IPlanFeatureStore>());

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.ShouldBeNull("IPlanIdProvider is not registered");
    }

    [Fact]
    public async Task GetOrNullAsync_NoPlanFeatureStore_ReturnsNull()
    {
        IPlanIdProvider planIdProvider = Substitute.For<IPlanIdProvider>();
        planIdProvider.GetCurrentPlanIdAsync(Arg.Any<CancellationToken>())
                      .Returns("enterprise");

        PlanFeatureValueProvider provider = BuildProvider(planIdProvider: planIdProvider);

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.ShouldBeNull("IPlanFeatureStore is not registered");
    }

    // -------------------------------------------------------------------------
    // GetOrNullAsync — plan ID resolution
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrNullAsync_PlanIdProviderReturnsNull_ReturnsNull()
    {
        IPlanIdProvider planIdProvider = Substitute.For<IPlanIdProvider>();
        planIdProvider.GetCurrentPlanIdAsync(Arg.Any<CancellationToken>())
                      .Returns((string?)null);

        IPlanFeatureStore planFeatureStore = Substitute.For<IPlanFeatureStore>();
        PlanFeatureValueProvider provider = BuildProvider(planIdProvider, planFeatureStore);

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.ShouldBeNull("plan ID is null — store should not be queried");
        await planFeatureStore.DidNotReceiveWithAnyArgs()
                              .GetOrNullAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetOrNullAsync_PlanHasValue_ReturnsValue()
    {
        IPlanIdProvider planIdProvider = Substitute.For<IPlanIdProvider>();
        planIdProvider.GetCurrentPlanIdAsync(Arg.Any<CancellationToken>())
                      .Returns("enterprise");

        IPlanFeatureStore planFeatureStore = Substitute.For<IPlanFeatureStore>();
        planFeatureStore.GetOrNullAsync("enterprise", "App.Feature", Arg.Any<CancellationToken>())
                        .Returns("true");

        PlanFeatureValueProvider provider = BuildProvider(planIdProvider, planFeatureStore);

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.ShouldBe("true");
    }

    [Fact]
    public async Task GetOrNullAsync_PlanHasNoOverride_ReturnsNull()
    {
        IPlanIdProvider planIdProvider = Substitute.For<IPlanIdProvider>();
        planIdProvider.GetCurrentPlanIdAsync(Arg.Any<CancellationToken>())
                      .Returns("starter");

        IPlanFeatureStore planFeatureStore = Substitute.For<IPlanFeatureStore>();
        planFeatureStore.GetOrNullAsync("starter", "App.Feature", Arg.Any<CancellationToken>())
                        .Returns((string?)null);

        PlanFeatureValueProvider provider = BuildProvider(planIdProvider, planFeatureStore);

        string? result = await provider.GetOrNullAsync(
            MakeDefinition(), TestContext.Current.CancellationToken);

        result.ShouldBeNull("the starter plan has no override for this feature");
    }
}
