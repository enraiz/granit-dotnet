using Granit.Features.Definitions;
using Shouldly;
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

        FeatureGroupDefinition group = context.AddGroup("TestApp");

        group.Name.ShouldBe("TestApp");
        group.DisplayName.ShouldBeNull();
    }

    [Fact]
    public void AddGroup_WithDisplayName_ReturnsGroupWithDisplayName()
    {
        FeatureDefinitionContext context = new();

        FeatureGroupDefinition group = context.AddGroup("TestApp", "Acme Features");

        group.DisplayName.ShouldBe("Acme Features");
    }

    [Fact]
    public void AddGroup_MultipleTimes_CreatesDistinctGroups()
    {
        FeatureDefinitionContext context = new();

        FeatureGroupDefinition group1 = context.AddGroup("TestApp");
        FeatureGroupDefinition group2 = context.AddGroup("Billing");

        group1.ShouldNotBeSameAs(group2);
    }

    // -------------------------------------------------------------------------
    // GetAllDefinitions
    // -------------------------------------------------------------------------

    [Fact]
    public void GetAllDefinitions_NoGroups_ReturnsEmpty()
    {
        FeatureDefinitionContext context = new();

        IEnumerable<FeatureDefinition> definitions = context.GetAllDefinitions();

        definitions.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllDefinitions_EmptyGroups_ReturnsEmpty()
    {
        FeatureDefinitionContext context = new();
        context.AddGroup("EmptyGroup");

        IEnumerable<FeatureDefinition> definitions = context.GetAllDefinitions();

        definitions.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllDefinitions_Flattens_AllGroupFeatures()
    {
        FeatureDefinitionContext context = new();
        FeatureGroupDefinition group1 = context.AddGroup("TestApp");
        group1.AddToggle("Acme.Video", defaultValue: true);
        group1.AddNumeric("Acme.MaxUsers", 100);

        FeatureGroupDefinition group2 = context.AddGroup("Billing");
        group2.AddToggle("Billing.Invoices");

        var definitions = context.GetAllDefinitions().ToList();

        definitions.Count.ShouldBe(3);
        IEnumerable<string> names = definitions.Select(d => d.Name);
        names.ShouldContain("Acme.Video");
        names.ShouldContain("Acme.MaxUsers");
        names.ShouldContain("Billing.Invoices");
    }
}
