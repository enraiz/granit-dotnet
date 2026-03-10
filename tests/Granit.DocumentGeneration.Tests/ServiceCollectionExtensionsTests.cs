using Granit.DocumentGeneration.Extensions;
using Granit.DocumentGeneration.Pipeline;
using Granit.Templating.Keys;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.DocumentGeneration.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddGranitDocumentGeneration
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitDocumentGeneration_Registers_IDocumentGenerator_Scoped()
    {
        ServiceCollection services = new();
        services.AddGranitDocumentGeneration();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IDocumentGenerator) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitDocumentGeneration_IDocumentGenerator_IsReplaceable_WithTryAdd()
    {
        ServiceCollection services = new();
        services.AddScoped<IDocumentGenerator>(_ => null!); // pre-register
        services.AddGranitDocumentGeneration();              // TryAdd must not replace

        services.Count(d => d.ServiceType == typeof(IDocumentGenerator))
                .ShouldBe(1, "TryAddScoped must not add a duplicate");
    }

    // -------------------------------------------------------------------------
    // AddDocumentRenderer
    // -------------------------------------------------------------------------

    [Fact]
    public void AddDocumentRenderer_Registers_As_Singleton()
    {
        ServiceCollection services = new();
        services.AddDocumentRenderer<FakeRenderer>();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IDocumentRenderer) &&
            d.ImplementationType == typeof(FakeRenderer) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddDocumentRenderer_CalledTwice_RegistersBothRenderers()
    {
        ServiceCollection services = new();
        services.AddDocumentRenderer<FakeRenderer>();
        services.AddDocumentRenderer<AnotherFakeRenderer>();

        services.Count(d => d.ServiceType == typeof(IDocumentRenderer))
                .ShouldBe(2);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private sealed class FakeRenderer : IDocumentRenderer
    {
        public bool CanRender(DocumentFormat targetFormat) => targetFormat == DocumentFormat.Html;

        public Task<DocumentResult> RenderAsync(
            string html, DocumentFormat targetFormat, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentResult(ReadOnlyMemory<byte>.Empty, targetFormat));
    }

    private sealed class AnotherFakeRenderer : IDocumentRenderer
    {
        public bool CanRender(DocumentFormat targetFormat) => targetFormat == DocumentFormat.Pdf;

        public Task<DocumentResult> RenderAsync(
            string html, DocumentFormat targetFormat, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentResult(ReadOnlyMemory<byte>.Empty, targetFormat));
    }
}
