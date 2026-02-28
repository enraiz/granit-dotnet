// =============================================================================
// CurrentTenantTests - Unit tests for ICurrentTenant / CurrentTenant
// =============================================================================

using Granit.MultiTenancy;
using Shouldly;
using Xunit;

namespace Granit.MultiTenancy.Tests;

public sealed class CurrentTenantTests
{
    private static CurrentTenant Create() => new();

    [Fact]
    public void Initially_IsAvailable_False_And_Id_Null()
    {
        CurrentTenant tenant = Create();

        tenant.IsAvailable.ShouldBeFalse();
        tenant.Id.ShouldBeNull();
        tenant.Name.ShouldBeNull();
    }

    [Fact]
    public void Change_Sets_Tenant_Context()
    {
        CurrentTenant tenant = Create();
        var id = Guid.NewGuid();

        using IDisposable _ = tenant.Change(id, "Acme");

        tenant.IsAvailable.ShouldBeTrue();
        tenant.Id.ShouldBe(id);
        tenant.Name.ShouldBe("Acme");
    }

    [Fact]
    public void Change_WithNull_Clears_Context()
    {
        CurrentTenant tenant = Create();

        using IDisposable _ = tenant.Change(null);

        tenant.IsAvailable.ShouldBeFalse();
        tenant.Id.ShouldBeNull();
    }

    [Fact]
    public void Dispose_Restores_Previous_Context()
    {
        CurrentTenant tenant = Create();
        var id = Guid.NewGuid();

        IDisposable scope = tenant.Change(id, "Acme");
        scope.Dispose();

        tenant.IsAvailable.ShouldBeFalse();
        tenant.Id.ShouldBeNull();
    }

    [Fact]
    public void Nested_Change_Restores_Outer_Context_On_Dispose()
    {
        CurrentTenant tenant = Create();
        var outerTenant = Guid.NewGuid();
        var innerTenant = Guid.NewGuid();

        using IDisposable outer = tenant.Change(outerTenant, "Outer");

        using (IDisposable inner = tenant.Change(innerTenant, "Inner"))
        {
            tenant.Id.ShouldBe(innerTenant);
            tenant.Name.ShouldBe("Inner");
        }

        // After disposing the inner scope, the outer scope is restored
        tenant.Id.ShouldBe(outerTenant);
        tenant.Name.ShouldBe("Outer");
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        CurrentTenant tenant = Create();
        var id = Guid.NewGuid();

        IDisposable scope = tenant.Change(id);
        scope.Dispose();
        scope.Dispose(); // second dispose must not throw or alter the context

        tenant.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task AsyncLocal_Isolates_Tasks()
    {
        // Two parallel tasks with different tenants
        // must not interfere with each other (AsyncLocal)
        CurrentTenant tenant = Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var taskA = Task.Run(async () =>
        {
            using IDisposable _ = tenant.Change(tenantA, "A");
            await Task.Delay(10, TestContext.Current.CancellationToken);
            tenant.Id.ShouldBe(tenantA, "task A must see its own tenant");
        }, TestContext.Current.CancellationToken);

        var taskB = Task.Run(async () =>
        {
            using IDisposable _ = tenant.Change(tenantB, "B");
            await Task.Delay(10, TestContext.Current.CancellationToken);
            tenant.Id.ShouldBe(tenantB, "task B must see its own tenant");
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(taskA, taskB);

        // The root context is intact
        tenant.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void Change_WithoutName_Sets_Id_Only()
    {
        CurrentTenant tenant = Create();
        var id = Guid.NewGuid();

        using IDisposable _ = tenant.Change(id);

        tenant.Id.ShouldBe(id);
        tenant.Name.ShouldBeNull();
        tenant.IsAvailable.ShouldBeTrue();
    }
}
