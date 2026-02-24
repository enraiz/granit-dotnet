using FluentAssertions;
using Granit.BackgroundJobs.Endpoints;
using Xunit;

namespace Granit.BackgroundJobs.Endpoints.Tests;

public sealed class BackgroundJobsEndpointsOptionsTests
{
    [Fact]
    public void RoutePrefix_Default_ShouldBeBackgroundJobs() =>
        new BackgroundJobsEndpointsOptions().RoutePrefix.Should().Be("background-jobs");

    [Fact]
    public void RequiredRole_Default_ShouldBeGranitBackgroundJobsAdmin() =>
        new BackgroundJobsEndpointsOptions().RequiredRole.Should().Be("granit-background-jobs-admin");

    [Fact]
    public void TagName_Default_ShouldBeBackgroundJobs() =>
        new BackgroundJobsEndpointsOptions().TagName.Should().Be("Background Jobs");
}
