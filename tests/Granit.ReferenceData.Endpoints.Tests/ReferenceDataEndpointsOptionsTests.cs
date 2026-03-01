using Shouldly;
using Xunit;

namespace Granit.ReferenceData.Endpoints.Tests;

public sealed class ReferenceDataEndpointsOptionsTests
{
    [Fact]
    public void Default_RoutePrefix_Is_ReferenceData()
    {
        ReferenceDataEndpointsOptions options = new();

        options.RoutePrefix.ShouldBe("reference-data");
    }

    [Fact]
    public void Default_TagName_Is_ReferenceData()
    {
        ReferenceDataEndpointsOptions options = new();

        options.TagName.ShouldBe("Reference Data");
    }

    [Fact]
    public void Default_AdminPolicyName_Is_Set()
    {
        ReferenceDataEndpointsOptions options = new();

        options.AdminPolicyName.ShouldBe("ReferenceData.Admin");
    }

    [Fact]
    public void Default_RequiredRole_Is_Set()
    {
        ReferenceDataEndpointsOptions options = new();

        options.RequiredRole.ShouldBe("granit-reference-data-admin");
    }
}
