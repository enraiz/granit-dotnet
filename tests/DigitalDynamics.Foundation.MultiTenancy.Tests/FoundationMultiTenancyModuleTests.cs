// =============================================================================
// FoundationMultiTenancyModuleTests - DI integration tests for the module
// =============================================================================
// Verifies the complete service wiring via AddFoundation<T>(),
// in the style of AbpIntegratedTest<T> in ABP Framework.
//
// Each test bootstraps the full module (Security + MultiTenancy)
// and resolves services from the real DI container.
// =============================================================================

using DigitalDynamics.Foundation.Core.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.MultiTenancy.Middleware;
using DigitalDynamics.Foundation.MultiTenancy.Pipeline;
using DigitalDynamics.Foundation.MultiTenancy.Resolvers;
using DigitalDynamics.Foundation.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalDynamics.Foundation.MultiTenancy.Tests;

public sealed class FoundationMultiTenancyModuleTests
{
    private static WebApplication BuildApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddFoundation<FoundationMultiTenancyModule>();
        return builder.Build();
    }

    // --- DI wiring ---

    [Fact]
    public void ICurrentTenant_Is_Resolvable_And_Singleton()
    {
        using WebApplication app = BuildApp();

        ICurrentTenant first = app.Services.GetRequiredService<ICurrentTenant>();
        ICurrentTenant second = app.Services.GetRequiredService<ICurrentTenant>();

        first.Should().NotBeNull();
        first.Should().BeOfType<CurrentTenant>();
        first.Should().BeSameAs(second, "ICurrentTenant must be a singleton");
    }

    [Fact]
    public void TenantResolverPipeline_Is_Resolvable_And_Singleton()
    {
        using WebApplication app = BuildApp();

        TenantResolverPipeline first = app.Services.GetRequiredService<TenantResolverPipeline>();
        TenantResolverPipeline second = app.Services.GetRequiredService<TenantResolverPipeline>();

        first.Should().NotBeNull();
        first.Should().BeSameAs(second, "TenantResolverPipeline must be a singleton");
    }

    [Fact]
    public void Two_TenantResolvers_Are_Registered()
    {
        using WebApplication app = BuildApp();

        IEnumerable<ITenantResolver> resolvers = app.Services.GetRequiredService<IEnumerable<ITenantResolver>>();

        resolvers.Should().HaveCount(2);
    }

    [Fact]
    public void Resolvers_Are_Ordered_Header_Before_Jwt()
    {
        using WebApplication app = BuildApp();

        List<ITenantResolver> ordered = app.Services
            .GetRequiredService<IEnumerable<ITenantResolver>>()
            .OrderBy(r => r.Order)
            .ToList();

        ordered[0].Should().BeOfType<HeaderTenantResolver>("Header (order=100) must precede JWT (order=200)");
        ordered[1].Should().BeOfType<JwtClaimTenantResolver>();
    }

    [Fact]
    public void TenantResolutionMiddleware_Is_Resolvable_As_Scoped()
    {
        using WebApplication app = BuildApp();
        using IServiceScope scope = app.Services.CreateScope();

        TenantResolutionMiddleware middleware = scope.ServiceProvider.GetRequiredService<TenantResolutionMiddleware>();

        middleware.Should().NotBeNull();
    }

    // --- Topological order ---

    [Fact]
    public void Module_Topological_Order_Has_Security_Before_MultiTenancy()
    {
        using WebApplication app = BuildApp();

        FoundationApplication foundationApp = app.Services.GetRequiredService<FoundationApplication>();

        foundationApp.GetModuleTypes().Should().ContainInOrder(
            typeof(FoundationSecurityModule),
            typeof(FoundationMultiTenancyModule));
    }

    // --- Functional test (AbpIntegratedTest style) ---

    [Fact]
    public void ICurrentTenant_Resolved_From_DI_Change_Works()
    {
        using WebApplication app = BuildApp();
        ICurrentTenant currentTenant = app.Services.GetRequiredService<ICurrentTenant>();
        Guid tenantId = Guid.NewGuid();

        currentTenant.IsAvailable.Should().BeFalse("no active tenant at startup");

        using (currentTenant.Change(tenantId, "Acme"))
        {
            currentTenant.IsAvailable.Should().BeTrue();
            currentTenant.Id.Should().Be(tenantId);
            currentTenant.Name.Should().Be("Acme");
        }

        currentTenant.IsAvailable.Should().BeFalse("the scope must be restored after Dispose");
    }

    [Fact]
    public void ICurrentTenant_Resolved_From_DI_Nested_Scopes_Work()
    {
        using WebApplication app = BuildApp();
        ICurrentTenant currentTenant = app.Services.GetRequiredService<ICurrentTenant>();
        Guid outer = Guid.NewGuid();
        Guid inner = Guid.NewGuid();

        using (currentTenant.Change(outer, "Outer"))
        {
            currentTenant.Id.Should().Be(outer);

            using (currentTenant.Change(inner, "Inner"))
            {
                currentTenant.Id.Should().Be(inner);
            }

            currentTenant.Id.Should().Be(outer, "the outer scope must be restored after the inner scope is disposed");
        }

        currentTenant.IsAvailable.Should().BeFalse();
    }
}
