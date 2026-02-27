using FluentAssertions;
using Granit.Templating.GlobalContext;
using Granit.Templating.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Granit.Templating.Scriban.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddGranitTemplatingWithScriban
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitTemplatingWithScriban_Registers_ITemplateEngine_Singleton()
    {
        ServiceCollection services = new();
        services.AddGranitTemplatingWithScriban();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITemplateEngine) &&
            d.ImplementationType == typeof(ScribanTemplateEngine) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitTemplatingWithScriban_Registers_ITextTemplateRenderer_Scoped()
    {
        ServiceCollection services = new();
        services.AddGranitTemplatingWithScriban();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITextTemplateRenderer) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitTemplatingWithScriban_Registers_Two_ITemplateGlobalContexts()
    {
        ServiceCollection services = new();
        services.AddGranitTemplatingWithScriban();

        // NowGlobalContext + ExecutionContextGlobalContext
        services.Count(d => d.ServiceType == typeof(ITemplateGlobalContext))
                .Should().Be(2, "NowGlobalContext and ExecutionContextGlobalContext must be registered");
    }

    [Fact]
    public void AddGranitTemplatingWithScriban_ITemplateEngine_IsReplaceable_WithTryAdd()
    {
        ServiceCollection services = new();
        services.AddSingleton<ITemplateEngine>(_ => null!); // pre-register custom engine
        services.AddGranitTemplatingWithScriban();           // TryAdd must not replace

        services.Count(d => d.ServiceType == typeof(ITemplateEngine))
                .Should().Be(1, "TryAddSingleton must not add a duplicate");
    }
}
