using FluentAssertions;
using Xunit;

namespace Granit.Persistence.Migrations.Tests;

public sealed class MigrationCycleAttributeTests
{
    [Fact]
    public void Constructor_SetsPhaseAndCycleId()
    {
        MigrationCycleAttribute attr = new(MigrationPhase.Expand, "patient-fullname-v2");

        attr.Phase.Should().Be(MigrationPhase.Expand);
        attr.CycleId.Should().Be("patient-fullname-v2");
    }

    [Fact]
    public void Constructor_ContractPhase_SetsCorrectly()
    {
        MigrationCycleAttribute attr = new(MigrationPhase.Contract, "patient-fullname-v2");

        attr.Phase.Should().Be(MigrationPhase.Contract);
    }

    [Fact]
    public void Attribute_IsNotAllowedMultipleTimes()
    {
        AttributeUsageAttribute? usage = typeof(MigrationCycleAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Attribute_TargetsClassOnly()
    {
        AttributeUsageAttribute? usage = typeof(MigrationCycleAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Class);
    }
}
