using Granit.BackgroundJobs.Abstractions;
using Shouldly;
using Xunit;

namespace Granit.BackgroundJobs.Tests;

public sealed class RecurringJobAttributeTests
{
    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        // Arrange / Act
        RecurringJobAttribute attr = new("0 * * * *", "hourly-cleanup");

        // Assert
        attr.CronExpression.ShouldBe("0 * * * *");
        attr.Name.ShouldBe("hourly-cleanup");
    }

    [Theory]
    [InlineData("", "hourly-cleanup")]
    [InlineData("   ", "hourly-cleanup")]
    public void Constructor_WhitespaceCron_ThrowsArgumentException(string cron, string name)
    {
        // Act
        Action act = () => _ = new RecurringJobAttribute(cron, name);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData("0 * * * *", "")]
    [InlineData("0 * * * *", "   ")]
    public void Constructor_WhitespaceName_ThrowsArgumentException(string cron, string name)
    {
        // Act
        Action act = () => _ = new RecurringJobAttribute(cron, name);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_CannotApplyTwice()
    {
        // Assert — validate AttributeUsage metadata
        AttributeUsageAttribute usage = typeof(RecurringJobAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.AllowMultiple.ShouldBeFalse();
        usage.ValidOn.HasFlag(AttributeTargets.Class).ShouldBeTrue();
    }
}
