using FluentAssertions;
using Granit.Core.MultiTenancy;
using Xunit;

namespace Granit.Core.Tests.MultiTenancy;

public sealed class NullTenantContextTests
{
    [Fact]
    public void IsAvailable_IsFalse() =>
        NullTenantContext.Instance.IsAvailable.Should().BeFalse();

    [Fact]
    public void Id_IsNull() =>
        NullTenantContext.Instance.Id.Should().BeNull();

    [Fact]
    public void Name_IsNull() =>
        NullTenantContext.Instance.Name.Should().BeNull();

    [Fact]
    public void Change_ReturnsDisposable_WithoutEffect()
    {
        using IDisposable scope = NullTenantContext.Instance.Change(Guid.NewGuid(), "tenant-a");

        scope.Should().NotBeNull();
        NullTenantContext.Instance.Id.Should().BeNull("Change is a no-op");
        NullTenantContext.Instance.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Change_WithNull_ReturnsDisposable()
    {
        using IDisposable scope = NullTenantContext.Instance.Change(null);

        scope.Should().NotBeNull();
    }

    [Fact]
    public void Instance_IsSingleton()
    {
        NullTenantContext a = NullTenantContext.Instance;
        NullTenantContext b = NullTenantContext.Instance;

        a.Should().BeSameAs(b);
    }

    [Fact]
    public void ImplementsICurrentTenant() =>
        NullTenantContext.Instance.Should().BeAssignableTo<ICurrentTenant>();
}
