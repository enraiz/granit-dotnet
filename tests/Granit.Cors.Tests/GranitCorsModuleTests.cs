using FluentAssertions;
using Granit.Core.Modularity;
using Granit.Cors.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.Cors.Tests;

public sealed class GranitCorsModuleTests
{
    [Fact]
    public void GranitCorsModule_IsGranitModule() =>
        typeof(GranitCorsModule).Should().BeAssignableTo<GranitModule>();

    [Fact]
    public void GranitCorsModule_IsSealed() =>
        typeof(GranitCorsModule).IsSealed.Should().BeTrue();

    [Fact]
    public void AddGranitCors_RegistersCorsOptionsValidator()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.AddGranitCors();

        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidateOptions<GranitCorsOptions>));
    }

    [Fact]
    public void AddGranitCors_RegistersCorsPolicyConfigurator()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.AddGranitCors();

        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<CorsOptions>));
    }

    [Fact]
    public void AddGranitCors_RegistersCorsServices()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.AddGranitCors();

        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICorsService));
    }
}
