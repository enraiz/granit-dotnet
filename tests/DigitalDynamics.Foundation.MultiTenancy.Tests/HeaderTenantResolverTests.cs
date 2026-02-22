// =============================================================================
// HeaderTenantResolverTests - Unit tests for the HTTP header tenant resolver
// =============================================================================

using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.MultiTenancy.Resolvers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.MultiTenancy.Tests;

public sealed class HeaderTenantResolverTests
{
    private static HeaderTenantResolver CreateResolver(string headerName = "X-Tenant-Id")
    {
        IOptions<MultiTenancyOptions> options = Options.Create(new MultiTenancyOptions
        {
            TenantIdHeaderName = headerName
        });
        return new HeaderTenantResolver(options);
    }

    private static DefaultHttpContext ContextWithHeader(string name, string value)
    {
        DefaultHttpContext context = new();
        context.Request.Headers.Append(name, value);
        return context;
    }

    [Fact]
    public void Order_Is_100()
    {
        HeaderTenantResolver resolver = CreateResolver();
        resolver.Order.Should().Be(100);
    }

    [Fact]
    public async Task ValidHeader_Returns_TenantInfo()
    {
        HeaderTenantResolver resolver = CreateResolver();
        var tenantId = Guid.NewGuid();
        DefaultHttpContext context = ContextWithHeader("X-Tenant-Id", tenantId.ToString());

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task MissingHeader_Returns_Null()
    {
        HeaderTenantResolver resolver = CreateResolver();
        DefaultHttpContext context = new();

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EmptyHeader_Returns_Null()
    {
        HeaderTenantResolver resolver = CreateResolver();
        DefaultHttpContext context = ContextWithHeader("X-Tenant-Id", string.Empty);

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidGuid_Returns_Null()
    {
        HeaderTenantResolver resolver = CreateResolver();
        DefaultHttpContext context = ContextWithHeader("X-Tenant-Id", "not-a-guid");

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CustomHeaderName_Is_Respected()
    {
        HeaderTenantResolver resolver = CreateResolver("X-Custom-Tenant");
        var tenantId = Guid.NewGuid();
        DefaultHttpContext context = ContextWithHeader("X-Custom-Tenant", tenantId.ToString());

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task WrongHeaderName_Returns_Null()
    {
        HeaderTenantResolver resolver = CreateResolver("X-Custom-Tenant");
        DefaultHttpContext context = ContextWithHeader("X-Tenant-Id", Guid.NewGuid().ToString());

        TenantInfo? result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }
}
