using FluentAssertions;
using Granit.Features.Definitions;
using Xunit;

namespace Granit.Features.Tests.Definitions;

public sealed class FeatureDefinitionContextTests
{
    // -------------------------------------------------------------------------
    // AddGroup
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGroup_ReturnsGroupWithName()
    {
        FeatureDefinitionContext context = new();

        FeatureGroupDefinition group = context.AddGroup("Guava");

        group.Name.Should().Be("Guava");
        group.DisplayName.Should().BeNull();
    }

    [Fact]
    public void AddGroup_WithDisplayName_ReturnsGroupWithDisplayName()
    {
        FeatureDefinitionContext context = new();

        FeatureGroupDefinition group = context.AddGroup("Guava", "Guava Features");

        group.DisplayName.Should().Be("Guava Features");
    }

    [Fact]
    public void AddGroup_MultipleTimes_CreatesDistinctGroups()
    {
        FeatureDefinitionContext context = new();

        FeatureGroupDefinition group1 = context.AddGroup("Guava");
        FeatureGroupDefinition group2 = context.AddGroup("Billing");

        group1.Should().NotBeSameAs(group2);
    }

    // -------------------------------------------------------------------------
    // GetAllDefinitions
    // -------------------------------------------------------------------------

    [Fact]
    public void GetAllDefinitions_NoGroups_ReturnsEmpty()
    {
        FeatureDefinitionContext context = new();

        IEnumerable<FeatureDefinition> definitions = context.GetAllDefinitions();

        definitions.Should().BeEmpty();
    }

    [Fact]
    public void GetAllDefinitions_EmptyGroups_ReturnsEmpty()
    {
        FeatureDefinitionContext context = new();
        context.AddGroup("EmptyGroup");

        IEnumerable<FeatureDefinition> definitions = context.GetAllDefinitions();

        definitions.Should().BeEmpty();
    }

    [Fact]
    public void GetAllDefinitions_Flattens_AllGroupFeatures()
    {
        FeatureDefinitionContext context = new();
        FeatureGroupDefinition group1 = context.AddGroup("Guava");
        group1.AddToggle("Guava.Video", defaultValue: true);
        group1.AddNumeric("Guava.MaxPatients", 100);

        FeatureGroupDefinition group2 = context.AddGroup("Billing");
        group2.AddToggle("Billing.Invoices");

        List<FeatureDefinition> definitions = context.GetAllDefinitions().ToList();

        definitions.Should().HaveCount(3);
        definitions.Select(d => d.Name).Should().Contain(
            ["Guava.Video", "Guava.MaxPatients", "Billing.Invoices"]);
    }
}
