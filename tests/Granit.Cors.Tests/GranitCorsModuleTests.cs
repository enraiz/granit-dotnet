using Granit.Core.Modularity;
using Granit.Cors.Extensions;
using Granit.Cors.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Cors.Tests;

public sealed class GranitCorsModuleTests
{
    [Fact]
    public void GranitCorsModule_IsGranitModule() =>
        typeof(GranitCorsModule).IsAssignableTo(typeof(GranitModule)).ShouldBeTrue();

    [Fact]
    public void GranitCorsModule_IsSealed() =>
        typeof(GranitCorsModule).IsSealed.ShouldBeTrue();

    [Fact]
    public void AddGranitCors_RegistersCorsOptionsValidator()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.AddGranitCors();

        builder.Services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IValidateOptions<GranitCorsOptions>));
    }

    [Fact]
    public void AddGranitCors_RegistersCorsPolicyConfigurator()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.AddGranitCors();

        builder.Services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<CorsOptions>));
    }

    [Fact]
    public void AddGranitCors_RegistersCorsServices()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.AddGranitCors();

        builder.Services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(ICorsService));
    }
}
