using Granit.Features.Definitions;
using Granit.Features.ValueTypes;
using Shouldly;
using Xunit;

namespace Granit.Features.Tests.Definitions;

public sealed class FeatureDefinitionTests
{
    // -------------------------------------------------------------------------
    // Constructor — valid
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        FeatureDefinition definition = new("App.Feature", "false", FeatureValueType.Toggle);

        definition.Name.ShouldBe("App.Feature");
        definition.DefaultValue.ShouldBe("false");
        definition.ValueType.ShouldBe(FeatureValueType.Toggle);
    }

    [Fact]
    public void Constructor_NumericType_SetsProperties()
    {
        FeatureDefinition definition = new("App.MaxPatients", "100", FeatureValueType.Numeric)
        {
            NumericConstraint = new NumericConstraint(0, 1000),
            DisplayName = "Max patients",
            Description = "Maximum number of patients per tenant",
        };

        definition.ValueType.ShouldBe(FeatureValueType.Numeric);
        definition.NumericConstraint.ShouldNotBeNull();
        definition.NumericConstraint!.Min.ShouldBe(0);
        definition.NumericConstraint.Max.ShouldBe(1000);
        definition.DisplayName.ShouldBe("Max patients");
        definition.Description.ShouldBe("Maximum number of patients per tenant");
    }

    [Fact]
    public void Constructor_SelectionType_SetsProperties()
    {
        SelectionValues selection = new(["starter", "professional", "enterprise"]);
        FeatureDefinition definition = new("App.Plan", "starter", FeatureValueType.Selection)
        {
            SelectionValues = selection,
        };

        definition.ValueType.ShouldBe(FeatureValueType.Selection);
        definition.SelectionValues.ShouldBeSameAs(selection);
    }

    [Fact]
    public void Constructor_OptionalProperties_DefaultToNull()
    {
        FeatureDefinition definition = new("App.Feature", "true", FeatureValueType.Toggle);

        definition.NumericConstraint.ShouldBeNull();
        definition.SelectionValues.ShouldBeNull();
        definition.DisplayName.ShouldBeNull();
        definition.Description.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // Constructor — validation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceName_Throws(string? name)
    {
        Action act = () => _ = new FeatureDefinition(name!, "false", FeatureValueType.Toggle);

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceDefaultValue_Throws(string? defaultValue)
    {
        Action act = () => _ = new FeatureDefinition("App.Feature", defaultValue!, FeatureValueType.Toggle);

        Should.Throw<ArgumentException>(act);
    }
}
