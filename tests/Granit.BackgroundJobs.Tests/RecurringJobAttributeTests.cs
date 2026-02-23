using FluentAssertions;
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
        attr.CronExpression.Should().Be("0 * * * *");
        attr.Name.Should().Be("hourly-cleanup");
    }

    [Theory]
    [InlineData("", "hourly-cleanup")]
    [InlineData("   ", "hourly-cleanup")]
    public void Constructor_WhitespaceCron_ThrowsArgumentException(string cron, string name)
    {
        // Act
        Action act = () => _ = new RecurringJobAttribute(cron, name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("0 * * * *", "")]
    [InlineData("0 * * * *", "   ")]
    public void Constructor_WhitespaceName_ThrowsArgumentException(string cron, string name)
    {
        // Act
        Action act = () => _ = new RecurringJobAttribute(cron, name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_CannotApplyTwice()
    {
        // Assert — validate AttributeUsage metadata
        AttributeUsageAttribute usage = typeof(RecurringJobAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.AllowMultiple.Should().BeFalse();
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }
}
