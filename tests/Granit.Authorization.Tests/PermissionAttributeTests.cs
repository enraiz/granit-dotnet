using FluentAssertions;
using Granit.Authorization.Attributes;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Granit.Authorization.Tests;

public sealed class PermissionAttributeTests
{
    [Fact]
    public void Constructor_SetsPolicy_ToPermissionName()
    {
        PermissionAttribute attr = new("Invoices.Delete");

        attr.Policy.Should().Be("Invoices.Delete");
    }

    [Fact]
    public void InheritsFromAuthorizeAttribute()
    {
        PermissionAttribute attr = new("Invoices.Read");

        attr.Should().BeAssignableTo<AuthorizeAttribute>();
    }

    [Fact]
    public void AllowsMultipleOnSameTarget()
    {
        AttributeUsageAttribute? usage = typeof(PermissionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeTrue();
    }
}
