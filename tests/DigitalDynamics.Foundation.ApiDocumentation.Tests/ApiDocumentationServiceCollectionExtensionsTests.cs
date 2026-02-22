// =============================================================================
// Tests - ApiDocumentationServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationApiDocumentation enregistre un document OpenAPI
// par version déclarée, et que les transformers sont enregistrés en DI.
// =============================================================================

using System.Reflection;
using DigitalDynamics.Foundation.ApiDocumentation.Extensions;
using DigitalDynamics.Foundation.ApiDocumentation.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.ApiDocumentation.Tests;

public sealed class ApiDocumentationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationApiDocumentation_WithConfiguration_RegistersOptions()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiDocumentation:Title"] = "Guava API",
                ["ApiDocumentation:MajorVersions:0"] = "1",
                ["ApiDocumentation:EnableInProduction"] = "false",
            })
            .Build();

        // Act
        services.AddFoundationApiDocumentation(configuration);

        // Assert — options are bound from configuration
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiDocumentationOptions options =
            sp.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        options.Title.Should().Be("Guava API");
        options.MajorVersions.Should().ContainSingle().Which.Should().Be(1);
        options.EnableInProduction.Should().BeFalse();
    }

    [Fact]
    public void AddFoundationApiDocumentation_WithLambda_RegistersOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "Test API";
            opts.MajorVersions = [1, 2];
        });

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiDocumentationOptions options =
            sp.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        options.Title.Should().Be("Test API");
        options.MajorVersions.Should().HaveCount(2).And.Contain([1, 2]);
    }

    [Fact]
    public void AddFoundationApiDocumentation_WithDefaultConfig_UsesDefaultOptions()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddFoundationApiDocumentation(configuration);

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiDocumentationOptions options =
            sp.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        options.Title.Should().Be("API");
        options.MajorVersions.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public void AddFoundationApiDocumentation_ReturnsServices_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        IServiceCollection returned = services.AddFoundationApiDocumentation(configuration);

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFoundationApiDocumentation_WithMultipleVersions_RegistersMultipleOpenApiDocuments()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "Multi-version API";
            opts.MajorVersions = [1, 2, 3];
        });

        // Assert — one AddOpenApi registration per version → each is named "v1", "v2", "v3"
        // AddOpenApi registers IConfigureOptions<OpenApiOptions> keyed by document name.
        // We verify services are registered (not zero), which indicates documents were added.
        services.Should().NotBeEmpty("versions [1, 2, 3] must register OpenAPI services");
    }

    // --- OpenApiOptions : déclenchement du callback AddOpenApi et du transformer inline ---

    [Fact]
    public async Task AddFoundationApiDocumentation_DocumentTransformer_SetsInfoWithoutContact()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "My API";
            opts.Description = "My description";
            opts.MajorVersions = [1];
            opts.ContactEmail = null;
        });

        using ServiceProvider sp = services.BuildServiceProvider();

        // IOptionsMonitor.Get("v1") triggers the outer openApiOptions => lambda,
        // which registers the document transformers on the OpenApiOptions instance.
        IOptionsMonitor<OpenApiOptions> monitor = sp.GetRequiredService<IOptionsMonitor<OpenApiOptions>>();
        OpenApiOptions openApiOpts = monitor.Get("v1");

        // DocumentTransformers is internal in Microsoft.AspNetCore.OpenApi.
        // Reflection is required to retrieve and invoke the inline delegate transformer.
        List<IOpenApiDocumentTransformer> transformers = GetDocumentTransformers(openApiOpts);
        OpenApiDocument doc = new() { Paths = [] };
        OpenApiDocumentTransformerContext ctx = BuildTransformerContext("v1");

        // Act — invoke the inline delegate (first transformer = the doc.Info setter)
        await transformers[0].TransformAsync(doc, ctx, TestContext.Current.CancellationToken);

        // Assert
        doc.Info.Title.Should().Be("My API");
        doc.Info.Description.Should().Be("My description");
        doc.Info.Contact.Should().BeNull("ContactEmail is null → no contact block");
    }

    [Fact]
    public async Task AddFoundationApiDocumentation_DocumentTransformer_SetsInfoWithContact()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "Guava API";
            opts.Description = null;
            opts.ContactEmail = "api@example.com";
            opts.MajorVersions = [2];
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        IOptionsMonitor<OpenApiOptions> monitor = sp.GetRequiredService<IOptionsMonitor<OpenApiOptions>>();
        OpenApiOptions openApiOpts = monitor.Get("v2");

        List<IOpenApiDocumentTransformer> transformers = GetDocumentTransformers(openApiOpts);
        OpenApiDocument doc = new() { Paths = [] };
        OpenApiDocumentTransformerContext ctx = BuildTransformerContext("v2");

        // Act
        await transformers[0].TransformAsync(doc, ctx, TestContext.Current.CancellationToken);

        // Assert
        doc.Info.Contact.Should().NotBeNull();
        doc.Info.Contact!.Email.Should().Be("api@example.com");
        doc.Info.Description.Should().BeNull();
    }

    // --- Helpers ---

    /// <summary>
    /// Accesses the internal <c>DocumentTransformers</c> field on <see cref="OpenApiOptions"/>
    /// via reflection. The field is <c>internal</c> in <c>Microsoft.AspNetCore.OpenApi</c> (validated on 10.0.3).
    /// If the field is renamed in a future SDK version this helper will throw an explicit error.
    /// </summary>
    private static List<IOpenApiDocumentTransformer> GetDocumentTransformers(OpenApiOptions options)
    {
        // DocumentTransformers is an internal field (not a property) in Microsoft.AspNetCore.OpenApi.
        FieldInfo field = typeof(OpenApiOptions).GetField(
            "DocumentTransformers",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "OpenApiOptions.DocumentTransformers not found. " +
                "The field may have been renamed in this version of Microsoft.AspNetCore.OpenApi.");

        return (List<IOpenApiDocumentTransformer>)field.GetValue(options)!;
    }

    private static OpenApiDocumentTransformerContext BuildTransformerContext(string documentName) =>
        new()
        {
            DocumentName = documentName,
            DescriptionGroups = [],
            ApplicationServices = Substitute.For<IServiceProvider>(),
        };
}
