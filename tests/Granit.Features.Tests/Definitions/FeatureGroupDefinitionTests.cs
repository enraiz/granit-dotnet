using Shouldly;
using Granit.Features.Definitions;
using Granit.Features.ValueTypes;
using Xunit;

namespace Granit.Features.Tests.Definitions;

public sealed class FeatureGroupDefinitionTests
{
    private static FeatureGroupDefinition MakeGroup(string name = "Guava", string? displayName = null)
    {
        // FeatureGroupDefinition constructor is internal — use IFeatureDefinitionContext
        // AddGroup returns the group directly, so capture it via a closure.
        FeatureGroupDefinition? group = null;
        FakeContextProvider provider = new(ctx =>
        {
            group = ctx.AddGroup(name, displayName);
        });
        FeatureDefinitionContext context = new();
        provider.Define(context);
        return group!;
    }

    private sealed class FakeContextProvider(Action<IFeatureDefinitionContext> define)
        : IFeatureDefinitionProvider
    {
        public void Define(IFeatureDefinitionContext context) => define(context);
    }

    [Fact]
    public void AddToggle_DefaultFalse_AddsDefinitionWithFalseDefault()
    {
        FeatureGroupDefinition group = MakeGroup();
        group.AddToggle("Guava.VideoConsultation");

        FeatureDefinition feature = group.Features.ShouldHaveSingleItem().Which;
        feature.Name.ShouldBe("Guava.VideoConsultation");
        feature.DefaultValue.ShouldBe("false");
        feature.ValueType.ShouldBe(FeatureValueType.Toggle);
    }

    [Fact]
    public void AddToggle_DefaultTrue_AddsDefinitionWithTrueDefault()
    {
        FeatureGroupDefinition group = MakeGroup();
        group.AddToggle("Guava.VideoConsultation", defaultValue: true, displayName: "Video");

        FeatureDefinition feature = group.Features.Single();
        feature.DefaultValue.ShouldBe("true");
        feature.DisplayName.ShouldBe("Video");
    }

    [Fact]
    public void AddNumeric_AddsDefinitionWithNumericConstraint()
    {
        FeatureGroupDefinition group = MakeGroup();
        group.AddNumeric("Guava.MaxPatients", defaultValue: 200, min: 0, max: 10000);

        FeatureDefinition feature = group.Features.Single();
        feature.DefaultValue.ShouldBe("200");
        feature.ValueType.ShouldBe(FeatureValueType.Numeric);
        feature.NumericConstraint.ShouldNotBeNull();
        feature.NumericConstraint!.Min.ShouldBe(0);
        feature.NumericConstraint.Max.ShouldBe(10000);
    }

    [Fact]
    public void AddSelection_AddsDefinitionWithSelectionValues()
    {
        FeatureGroupDefinition group = MakeGroup();
        group.AddSelection(
            "Guava.StorageTier",
            defaultValue: "standard",
            allowedValues: ["standard", "premium"],
            displayName: "Storage");

        FeatureDefinition feature = group.Features.Single();
        feature.DefaultValue.ShouldBe("standard");
        feature.ValueType.ShouldBe(FeatureValueType.Selection);
        feature.DisplayName.ShouldBe("Storage");
        feature.SelectionValues.ShouldNotBeNull();
        feature.SelectionValues!.AllowedValues.ShouldBe(["standard", "premium"]);
    }

    [Fact]
    public void AddToggle_IsChainable_ReturnsGroup()
    {
        FeatureGroupDefinition group = MakeGroup();

        FeatureGroupDefinition returned = group
            .AddToggle("Guava.FeatureA")
            .AddToggle("Guava.FeatureB");

        returned.ShouldBeSameAs(group);
        group.Features.Count.ShouldBe(2);
    }

    [Fact]
    public void Group_DisplayName_IsPreserved()
    {
        FeatureGroupDefinition group = MakeGroup("Guava", "Guava Platform");

        group.Name.ShouldBe("Guava");
        group.DisplayName.ShouldBe("Guava Platform");
    }
}
