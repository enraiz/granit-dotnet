using Granit.Templating.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.DocumentGeneration.Excel.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddGranitDocumentGenerationExcel
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitDocumentGenerationExcel_Registers_ITemplateEngine_Singleton()
    {
        ServiceCollection services = new();
        services.AddGranitDocumentGenerationExcel();

        services.ShouldContain(d =>
            d.ServiceType == typeof(ITemplateEngine) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitDocumentGenerationExcel_CalledTwice_RegistersBothEngines()
    {
        // Additive registration — allows combining with other engines (Scriban, etc.)
        ServiceCollection services = new();
        services.AddGranitDocumentGenerationExcel();
        services.AddGranitDocumentGenerationExcel();

        services.Count(d => d.ServiceType == typeof(ITemplateEngine))
                .ShouldBe(2, "each call must add an independent registration");
    }
}
