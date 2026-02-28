using FluentAssertions;
using Granit.DocumentGeneration.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.DocumentGeneration.Pdf.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitDocumentGenerationPdf_Registers_IDocumentRenderer()
    {
        ServiceCollection services = CreateServices();
        services.AddGranitDocumentGenerationPdf();

        ServiceProvider provider = services.BuildServiceProvider();

        IDocumentRenderer? renderer = provider.GetService<IDocumentRenderer>();
        renderer.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitDocumentGenerationPdf_Registers_HostedService()
    {
        ServiceCollection services = CreateServices();
        services.AddGranitDocumentGenerationPdf();

        ServiceProvider provider = services.BuildServiceProvider();

        IEnumerable<IHostedService> hostedServices = provider.GetServices<IHostedService>();
        hostedServices.Should().ContainSingle();
    }

    [Fact]
    public void AddGranitDocumentGenerationPdf_Registers_PdfRenderOptions()
    {
        ServiceCollection services = CreateServices();
        services.AddGranitDocumentGenerationPdf();

        ServiceProvider provider = services.BuildServiceProvider();

        IOptions<PdfRenderOptions>? options = provider.GetService<IOptions<PdfRenderOptions>>();
        options.Should().NotBeNull();
        options!.Value.PaperFormat.Should().Be("A4");
    }

    [Fact]
    public void AddGranitDocumentGenerationPdf_Renderer_IsSingleton()
    {
        ServiceCollection services = CreateServices();
        services.AddGranitDocumentGenerationPdf();

        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDocumentRenderer));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    private static ServiceCollection CreateServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(configuration);
        return services;
    }
}
