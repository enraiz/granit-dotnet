using FluentAssertions;
using Granit.Templating.Enrichment;
using Granit.Templating.GlobalContext;
using Granit.Templating.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Granit.Templating.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddGranitTemplating
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitTemplating_Registers_ITextTemplateRenderer_Scoped()
    {
        ServiceCollection services = new();
        services.AddGranitTemplating();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITextTemplateRenderer) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitTemplating_ITextTemplateRenderer_IsReplaceable_WithTryAdd()
    {
        ServiceCollection services = new();
        services.AddScoped<ITextTemplateRenderer>(_ => null!); // pre-register
        services.AddGranitTemplating();                         // TryAdd must not replace

        services.Count(d => d.ServiceType == typeof(ITextTemplateRenderer))
                .Should().Be(1, "TryAddScoped must not add a duplicate");
    }

    // -------------------------------------------------------------------------
    // AddEmbeddedTemplates
    // -------------------------------------------------------------------------

    [Fact]
    public void AddEmbeddedTemplates_Registers_ITemplateResolver_Singleton()
    {
        ServiceCollection services = new();
        services.AddEmbeddedTemplates(typeof(ServiceCollectionExtensionsTests).Assembly);

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITemplateResolver) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEmbeddedTemplates_CalledTwice_RegistersBothResolvers()
    {
        ServiceCollection services = new();
        services.AddEmbeddedTemplates(typeof(ServiceCollectionExtensionsTests).Assembly);
        services.AddEmbeddedTemplates(typeof(object).Assembly);

        services.Count(d => d.ServiceType == typeof(ITemplateResolver))
                .Should().Be(2);
    }

    // -------------------------------------------------------------------------
    // AddTemplateGlobalContext
    // -------------------------------------------------------------------------

    [Fact]
    public void AddTemplateGlobalContext_Registers_As_Singleton()
    {
        ServiceCollection services = new();
        services.AddTemplateGlobalContext<FakeGlobalContext>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITemplateGlobalContext) &&
            d.ImplementationType == typeof(FakeGlobalContext) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    // -------------------------------------------------------------------------
    // AddTemplateDataEnricher
    // -------------------------------------------------------------------------

    [Fact]
    public void AddTemplateDataEnricher_Registers_As_Transient()
    {
        ServiceCollection services = new();
        services.AddTemplateDataEnricher<string, FakeEnricher>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITemplateDataEnricher<string>) &&
            d.ImplementationType == typeof(FakeEnricher) &&
            d.Lifetime == ServiceLifetime.Transient);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private sealed class FakeGlobalContext : ITemplateGlobalContext
    {
        public string ContextName => "fake";
        public object Resolve() => new { };
    }

    private sealed class FakeEnricher : ITemplateDataEnricher<string>
    {
        public int Order => 0;
        public Task<string> EnrichAsync(string data, CancellationToken ct = default) =>
            Task.FromResult(data);
    }
}
