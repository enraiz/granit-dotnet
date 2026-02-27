using FluentAssertions;
using Granit.Features.Definitions;
using Granit.Features.ValueTypes;
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

        definition.Name.Should().Be("App.Feature");
        definition.DefaultValue.Should().Be("false");
        definition.ValueType.Should().Be(FeatureValueType.Toggle);
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

        definition.ValueType.Should().Be(FeatureValueType.Numeric);
        definition.NumericConstraint.Should().NotBeNull();
        definition.NumericConstraint!.Min.Should().Be(0);
        definition.NumericConstraint.Max.Should().Be(1000);
        definition.DisplayName.Should().Be("Max patients");
        definition.Description.Should().Be("Maximum number of patients per tenant");
    }

    [Fact]
    public void Constructor_SelectionType_SetsProperties()
    {
        SelectionValues selection = new(["starter", "professional", "enterprise"]);
        FeatureDefinition definition = new("App.Plan", "starter", FeatureValueType.Selection)
        {
            SelectionValues = selection,
        };

        definition.ValueType.Should().Be(FeatureValueType.Selection);
        definition.SelectionValues.Should().BeSameAs(selection);
    }

    [Fact]
    public void Constructor_OptionalProperties_DefaultToNull()
    {
        FeatureDefinition definition = new("App.Feature", "true", FeatureValueType.Toggle);

        definition.NumericConstraint.Should().BeNull();
        definition.SelectionValues.Should().BeNull();
        definition.DisplayName.Should().BeNull();
        definition.Description.Should().BeNull();
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

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceDefaultValue_Throws(string? defaultValue)
    {
        Action act = () => _ = new FeatureDefinition("App.Feature", defaultValue!, FeatureValueType.Toggle);

        act.Should().Throw<ArgumentException>();
    }
}
