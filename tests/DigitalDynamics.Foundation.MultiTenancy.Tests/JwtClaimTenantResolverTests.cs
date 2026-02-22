// =============================================================================
// JwtClaimTenantResolverTests - Unit tests for the JWT tenant resolver
// =============================================================================

using System.Security.Claims;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.MultiTenancy.Resolvers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.MultiTenancy.Tests;

public sealed class JwtClaimTenantResolverTests
{
    private static JwtClaimTenantResolver CreateResolver(string claimType = "tenant_id")
    {
        IOptions<MultiTenancyOptions> options = Options.Create(new MultiTenancyOptions
        {
            TenantIdClaimType = claimType
        });
        return new JwtClaimTenantResolver(options);
    }

    private static DefaultHttpContext ContextWithClaim(string claimType, string claimValue)
    {
        DefaultHttpContext context = new();
        ClaimsIdentity identity = new([new Claim(claimType, claimValue)], "test");
        context.User = new ClaimsPrincipal(identity);
        return context;
    }

    [Fact]
    public void Order_Is_200()
    {
        JwtClaimTenantResolver resolver = CreateResolver();
        resolver.Order.Should().Be(200);
    }

    [Fact]
    public async Task ValidClaim_Returns_TenantInfo()
    {
        JwtClaimTenantResolver resolver = CreateResolver();
        Guid tenantId = Guid.NewGuid();
        DefaultHttpContext context = ContextWithClaim("tenant_id", tenantId.ToString());

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task MissingClaim_Returns_Null()
    {
        JwtClaimTenantResolver resolver = CreateResolver();
        DefaultHttpContext context = new();

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EmptyClaim_Returns_Null()
    {
        JwtClaimTenantResolver resolver = CreateResolver();
        DefaultHttpContext context = ContextWithClaim("tenant_id", string.Empty);

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidGuidClaim_Returns_Null()
    {
        JwtClaimTenantResolver resolver = CreateResolver();
        DefaultHttpContext context = ContextWithClaim("tenant_id", "not-a-guid");

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CustomClaimType_Is_Respected()
    {
        JwtClaimTenantResolver resolver = CreateResolver("custom_tenant");
        Guid tenantId = Guid.NewGuid();
        DefaultHttpContext context = ContextWithClaim("custom_tenant", tenantId.ToString());

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task WrongClaimType_Returns_Null()
    {
        JwtClaimTenantResolver resolver = CreateResolver("custom_tenant");
        DefaultHttpContext context = ContextWithClaim("tenant_id", Guid.NewGuid().ToString());

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UnauthenticatedUser_Returns_Null()
    {
        JwtClaimTenantResolver resolver = CreateResolver();
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal();

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }
}
