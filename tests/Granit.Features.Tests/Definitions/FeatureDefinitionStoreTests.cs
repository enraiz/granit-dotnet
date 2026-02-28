using Shouldly;
using Granit.Features.Definitions;
using Granit.Features.Exceptions;
using Granit.Features.ValueTypes;
using Xunit;

namespace Granit.Features.Tests.Definitions;

public sealed class FeatureDefinitionStoreTests
{
    private sealed class FakeProvider(params FeatureDefinition[] definitions) : IFeatureDefinitionProvider
    {
        public void Define(IFeatureDefinitionContext context)
        {
            FeatureGroupDefinition group = context.AddGroup("Test");
            foreach (FeatureDefinition def in definitions)
            {
                group.AddToggle(def.Name, defaultValue: def.DefaultValue == "true");
            }
        }
    }

    private sealed class DirectProvider(Action<IFeatureDefinitionContext> define) : IFeatureDefinitionProvider
    {
        public void Define(IFeatureDefinitionContext context) => define(context);
    }

    [Fact]
    public void NoProviders_GetAll_Returns_EmptyList()
    {
        FeatureDefinitionStore store = new([]);

        store.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public void GetRequired_KnownFeature_Returns_Definition()
    {
        FeatureDefinitionStore store = new([new DirectProvider(ctx =>
        {
            FeatureGroupDefinition group = ctx.AddGroup("App");
            group.AddToggle("App.VideoConsultation", defaultValue: false);
        })]);

        FeatureDefinition result = store.GetRequired("App.VideoConsultation");

        result.Name.ShouldBe("App.VideoConsultation");
        result.ValueType.ShouldBe(FeatureValueType.Toggle);
    }

    [Fact]
    public void GetRequired_UnknownFeature_Throws_FeatureNotFoundException()
    {
        FeatureDefinitionStore store = new([]);

        Action act = () => store.GetRequired("Unknown.Feature");

        Should.Throw<FeatureNotFoundException>(act).Message.ShouldContain("Unknown.Feature");
    }

    [Fact]
    public void GetOrNull_UnknownFeature_Returns_Null()
    {
        FeatureDefinitionStore store = new([]);

        FeatureDefinition? result = store.GetOrNull("Unknown.Feature");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetAll_Returns_AllDeclaredDefinitions()
    {
        FeatureDefinitionStore store = new([new DirectProvider(ctx =>
        {
            FeatureGroupDefinition group = ctx.AddGroup("App");
            group.AddToggle("App.VideoConsultation", defaultValue: false);
            group.AddNumeric("App.MaxPatients", defaultValue: 50, min: 1, max: 10_000);
        })]);

        IReadOnlyList<FeatureDefinition> all = store.GetAll();

        all.Count.ShouldBe(2);
        all.Select(d => d.Name).ShouldContain(["App.VideoConsultation", "App.MaxPatients"]);
    }

    [Fact]
    public void MultipleProviders_AggregatesAllDefinitions()
    {
        FeatureDefinitionStore store = new([
            new DirectProvider(ctx =>
            {
                FeatureGroupDefinition g = ctx.AddGroup("ModuleA");
                g.AddToggle("ModuleA.FeatureX", defaultValue: true);
            }),
            new DirectProvider(ctx =>
            {
                FeatureGroupDefinition g = ctx.AddGroup("ModuleB");
                g.AddToggle("ModuleB.FeatureY", defaultValue: false);
            })
        ]);

        store.GetAll().Count.ShouldBe(2);
        store.GetOrNull("ModuleA.FeatureX").ShouldNotBeNull();
        store.GetOrNull("ModuleB.FeatureY").ShouldNotBeNull();
    }
}
