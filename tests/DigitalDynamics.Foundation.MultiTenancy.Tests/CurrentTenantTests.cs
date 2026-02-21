// =============================================================================
// CurrentTenantTests - Tests unitaires de ICurrentTenant / CurrentTenant
// =============================================================================

using DigitalDynamics.Foundation.MultiTenancy;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.MultiTenancy.Tests;

public sealed class CurrentTenantTests
{
    private static CurrentTenant Create() => new();

    [Fact]
    public void Initially_IsAvailable_False_And_Id_Null()
    {
        CurrentTenant tenant = Create();

        tenant.IsAvailable.Should().BeFalse();
        tenant.Id.Should().BeNull();
        tenant.Name.Should().BeNull();
    }

    [Fact]
    public void Change_Sets_Tenant_Context()
    {
        CurrentTenant tenant = Create();
        Guid id = Guid.NewGuid();

        using IDisposable _ = tenant.Change(id, "Acme");

        tenant.IsAvailable.Should().BeTrue();
        tenant.Id.Should().Be(id);
        tenant.Name.Should().Be("Acme");
    }

    [Fact]
    public void Change_WithNull_Clears_Context()
    {
        CurrentTenant tenant = Create();

        using IDisposable _ = tenant.Change(null);

        tenant.IsAvailable.Should().BeFalse();
        tenant.Id.Should().BeNull();
    }

    [Fact]
    public void Dispose_Restores_Previous_Context()
    {
        CurrentTenant tenant = Create();
        Guid id = Guid.NewGuid();

        IDisposable scope = tenant.Change(id, "Acme");
        scope.Dispose();

        tenant.IsAvailable.Should().BeFalse();
        tenant.Id.Should().BeNull();
    }

    [Fact]
    public void Nested_Change_Restores_Outer_Context_On_Dispose()
    {
        CurrentTenant tenant = Create();
        Guid outerTenant = Guid.NewGuid();
        Guid innerTenant = Guid.NewGuid();

        using IDisposable outer = tenant.Change(outerTenant, "Outer");

        using (IDisposable inner = tenant.Change(innerTenant, "Inner"))
        {
            tenant.Id.Should().Be(innerTenant);
            tenant.Name.Should().Be("Inner");
        }

        // Après dispose du scope interne, le scope externe est restauré
        tenant.Id.Should().Be(outerTenant);
        tenant.Name.Should().Be("Outer");
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        CurrentTenant tenant = Create();
        Guid id = Guid.NewGuid();

        IDisposable scope = tenant.Change(id);
        scope.Dispose();
        scope.Dispose(); // second dispose ne doit pas planter ni altérer le contexte

        tenant.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task AsyncLocal_Isolates_Tasks()
    {
        // Deux tâches parallèles avec des tenants différents
        // ne doivent pas interférer (AsyncLocal)
        CurrentTenant tenant = Create();
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        Task taskA = Task.Run(async () =>
        {
            using IDisposable _ = tenant.Change(tenantA, "A");
            await Task.Delay(10, TestContext.Current.CancellationToken);
            tenant.Id.Should().Be(tenantA, "la tâche A doit voir son propre tenant");
        }, TestContext.Current.CancellationToken);

        Task taskB = Task.Run(async () =>
        {
            using IDisposable _ = tenant.Change(tenantB, "B");
            await Task.Delay(10, TestContext.Current.CancellationToken);
            tenant.Id.Should().Be(tenantB, "la tâche B doit voir son propre tenant");
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(taskA, taskB);

        // Le contexte racine est intact
        tenant.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Change_WithoutName_Sets_Id_Only()
    {
        CurrentTenant tenant = Create();
        Guid id = Guid.NewGuid();

        using IDisposable _ = tenant.Change(id);

        tenant.Id.Should().Be(id);
        tenant.Name.Should().BeNull();
        tenant.IsAvailable.Should().BeTrue();
    }
}
