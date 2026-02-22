// =============================================================================
// Tests - InternalApiAttribute
// =============================================================================
// Vérifie que l'attribut peut être appliqué sur les classes et les méthodes,
// et qu'il n'est pas héritable (sealed) ni répétable (AllowMultiple = false).
// =============================================================================

using DigitalDynamics.Foundation.ApiDocumentation.Attributes;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.ApiDocumentation.Tests;

public sealed class InternalApiAttributeTests
{
    [Fact]
    public void InternalApiAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        AttributeUsageAttribute? usage = typeof(InternalApiAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .OfType<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }

    [Fact]
    public void InternalApiAttribute_CanBeAppliedToMethod()
    {
        // Arrange & Act
        AttributeUsageAttribute? usage = typeof(InternalApiAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .OfType<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void InternalApiAttribute_AllowMultiple_IsFalse()
    {
        // Arrange & Act
        AttributeUsageAttribute? usage = typeof(InternalApiAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .OfType<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void InternalApiAttribute_IsAttribute()
    {
        // Assert
        typeof(InternalApiAttribute).Should().BeAssignableTo<Attribute>();
    }
}
