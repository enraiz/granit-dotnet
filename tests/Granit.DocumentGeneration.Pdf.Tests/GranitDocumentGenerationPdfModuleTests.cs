using FluentAssertions;
using Granit.Core.Modularity;
using Granit.DocumentGeneration.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace Granit.DocumentGeneration.Pdf.Tests;

public sealed class GranitDocumentGenerationPdfModuleTests
{
    [Fact]
    public void ConfigureServices_Registers_IDocumentRenderer()
    {
        ServiceCollection services = new();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(configuration);
        IHostApplicationBuilder builder = Substitute.For<IHostApplicationBuilder>();
        builder.Services.Returns(services);

        GranitDocumentGenerationPdfModule module = new();
        ServiceConfigurationContext context = new(services, configuration, builder);
        module.ConfigureServices(context);

        ServiceProvider provider = services.BuildServiceProvider();
        IDocumentRenderer? renderer = provider.GetService<IDocumentRenderer>();
        renderer.Should().NotBeNull();
    }

    [Fact]
    public void Module_DependsOn_GranitDocumentGenerationModule()
    {
        DependsOnAttribute[] attributes = typeof(GranitDocumentGenerationPdfModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), inherit: false)
            .Cast<DependsOnAttribute>()
            .ToArray();

        attributes.Should().ContainSingle();
        attributes[0].DependedTypes.Should().Contain(typeof(GranitDocumentGenerationModule));
    }
}
