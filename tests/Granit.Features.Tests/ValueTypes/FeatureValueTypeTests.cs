using Granit.Features.ValueTypes;
using Shouldly;
using Xunit;

namespace Granit.Features.Tests.ValueTypes;

public sealed class FeatureValueTypeTests
{
    [Fact]
    public void Toggle_HasExpectedValue() =>
        ((int)FeatureValueType.Toggle).ShouldBe(0);

    [Fact]
    public void Numeric_HasExpectedValue() =>
        ((int)FeatureValueType.Numeric).ShouldBe(1);

    [Fact]
    public void Selection_HasExpectedValue() =>
        ((int)FeatureValueType.Selection).ShouldBe(2);

    [Fact]
    public void Enum_HasExactlyThreeMembers()
    {
        string[] names = Enum.GetNames<FeatureValueType>();

        names.Length.ShouldBe(3);
        names.ShouldContain("Toggle");
        names.ShouldContain("Numeric");
        names.ShouldContain("Selection");
    }

    [Theory]
    [InlineData(FeatureValueType.Toggle, "Toggle")]
    [InlineData(FeatureValueType.Numeric, "Numeric")]
    [InlineData(FeatureValueType.Selection, "Selection")]
    public void ToString_ReturnsExpectedName(FeatureValueType type, string expected) =>
        type.ToString().ShouldBe(expected);
}
