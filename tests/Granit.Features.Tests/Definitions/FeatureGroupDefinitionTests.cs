using FluentAssertions;
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

        FeatureDefinition feature = group.Features.Should().ContainSingle().Which;
        feature.Name.Should().Be("Guava.VideoConsultation");
        feature.DefaultValue.Should().Be("false");
        feature.ValueType.Should().Be(FeatureValueType.Toggle);
    }

    [Fact]
    public void AddToggle_DefaultTrue_AddsDefinitionWithTrueDefault()
    {
        FeatureGroupDefinition group = MakeGroup();
        group.AddToggle("Guava.VideoConsultation", defaultValue: true, displayName: "Video");

        FeatureDefinition feature = group.Features.Single();
        feature.DefaultValue.Should().Be("true");
        feature.DisplayName.Should().Be("Video");
    }

    [Fact]
    public void AddNumeric_AddsDefinitionWithNumericConstraint()
    {
        FeatureGroupDefinition group = MakeGroup();
        group.AddNumeric("Guava.MaxPatients", defaultValue: 200, min: 0, max: 10000);

        FeatureDefinition feature = group.Features.Single();
        feature.DefaultValue.Should().Be("200");
        feature.ValueType.Should().Be(FeatureValueType.Numeric);
        feature.NumericConstraint.Should().NotBeNull();
        feature.NumericConstraint!.Min.Should().Be(0);
        feature.NumericConstraint.Max.Should().Be(10000);
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
        feature.DefaultValue.Should().Be("standard");
        feature.ValueType.Should().Be(FeatureValueType.Selection);
        feature.DisplayName.Should().Be("Storage");
        feature.SelectionValues.Should().NotBeNull();
        feature.SelectionValues!.AllowedValues.Should().BeEquivalentTo(["standard", "premium"]);
    }

    [Fact]
    public void AddToggle_IsChainable_ReturnsGroup()
    {
        FeatureGroupDefinition group = MakeGroup();

        FeatureGroupDefinition returned = group
            .AddToggle("Guava.FeatureA")
            .AddToggle("Guava.FeatureB");

        returned.Should().BeSameAs(group);
        group.Features.Should().HaveCount(2);
    }

    [Fact]
    public void Group_DisplayName_IsPreserved()
    {
        FeatureGroupDefinition group = MakeGroup("Guava", "Guava Platform");

        group.Name.Should().Be("Guava");
        group.DisplayName.Should().Be("Guava Platform");
    }
}
