using Granit.DocumentGeneration.Pdf.Extensions;
using Granit.DocumentGeneration.Pdf.Options;
using Granit.DocumentGeneration.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
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
        renderer.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitDocumentGenerationPdf_Registers_HostedService()
    {
        ServiceCollection services = CreateServices();
        services.AddGranitDocumentGenerationPdf();

        ServiceProvider provider = services.BuildServiceProvider();

        IEnumerable<IHostedService> hostedServices = provider.GetServices<IHostedService>();
        hostedServices.Count().ShouldBe(1);
    }

    [Fact]
    public void AddGranitDocumentGenerationPdf_Registers_PdfRenderOptions()
    {
        ServiceCollection services = CreateServices();
        services.AddGranitDocumentGenerationPdf();

        ServiceProvider provider = services.BuildServiceProvider();

        IOptions<PdfRenderOptions>? options = provider.GetService<IOptions<PdfRenderOptions>>();
        options.ShouldNotBeNull();
        options!.Value.PaperFormat.ShouldBe("A4");
    }

    [Fact]
    public void AddGranitDocumentGenerationPdf_Renderer_IsSingleton()
    {
        ServiceCollection services = CreateServices();
        services.AddGranitDocumentGenerationPdf();

        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDocumentRenderer));
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
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
