// =============================================================================
// Tests - JwtBearerSecuritySchemeTransformer
// =============================================================================
// Vérifie que le transformer ajoute le schéma Bearer et les exigences de
// sécurité aux opérations quand JWT Bearer est configuré, et est no-op sinon.
// =============================================================================

using System.Net.Http;
using DigitalDynamics.Foundation.ApiDocumentation.Transformers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.ApiDocumentation.Tests;

public sealed class JwtBearerSecuritySchemeTransformerTests
{
    // --- No JWT Bearer scheme registered ---

    [Fact]
    public async Task TransformAsync_NoJwtBearerScheme_DocumentUnchanged()
    {
        // Arrange
        IAuthenticationSchemeProvider provider = Substitute.For<IAuthenticationSchemeProvider>();
        provider.GetAllSchemesAsync().Returns([]);

        JwtBearerSecuritySchemeTransformer transformer = new(provider);
        OpenApiDocument document = new() { Paths = new OpenApiPaths() };
        OpenApiDocumentTransformerContext context = BuildContext();

        // Act
        await transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        document.Components.Should().BeNull("no JWT Bearer scheme → no security scheme added");
    }

    // --- JWT Bearer scheme registered ---

    [Fact]
    public async Task TransformAsync_JwtBearerScheme_SecuritySchemeAdded()
    {
        // Arrange
        IAuthenticationSchemeProvider provider = BuildProviderWithBearer();
        JwtBearerSecuritySchemeTransformer transformer = new(provider);
        OpenApiDocument document = new() { Paths = new OpenApiPaths() };
        OpenApiDocumentTransformerContext context = BuildContext();

        // Act
        await transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        document.Components.Should().NotBeNull();
        document.Components!.SecuritySchemes.Should().ContainKey("Bearer");
        OpenApiSecurityScheme scheme = (OpenApiSecurityScheme)document.Components.SecuritySchemes["Bearer"];
        scheme.Type.Should().Be(SecuritySchemeType.Http);
        scheme.Scheme.Should().Be("bearer");
        scheme.BearerFormat.Should().Be("JWT");
    }

    // --- Security requirement added to operations ---

    [Fact]
    public async Task TransformAsync_JwtBearerSchemeWithOperations_SecurityRequirementAddedToAllOperations()
    {
        // Arrange
        IAuthenticationSchemeProvider provider = BuildProviderWithBearer();
        JwtBearerSecuritySchemeTransformer transformer = new(provider);

        OpenApiOperation operation = new() { Summary = "Get items" };
        OpenApiPathItem pathItem = new();
        pathItem.Operations = new Dictionary<HttpMethod, OpenApiOperation>
        {
            [HttpMethod.Get] = operation,
        };

        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["/api/v1/items"] = pathItem,
            },
        };

        OpenApiDocumentTransformerContext context = BuildContext();

        // Act
        await transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        operation.Security.Should().NotBeNullOrEmpty("Bearer scheme adds a security requirement");
    }

    // --- Paths with null operations do not throw ---

    [Fact]
    public async Task TransformAsync_JwtBearerSchemeWithNullOperations_DoesNotThrow()
    {
        // Arrange
        IAuthenticationSchemeProvider provider = BuildProviderWithBearer();
        JwtBearerSecuritySchemeTransformer transformer = new(provider);

        OpenApiPathItem pathItemWithNullOps = new() { Operations = null };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["/api/v1/items"] = pathItemWithNullOps,
            },
        };

        OpenApiDocumentTransformerContext context = BuildContext();

        // Act
        Func<Task> act = () => transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- Helpers ---

    private static IAuthenticationSchemeProvider BuildProviderWithBearer()
    {
        IAuthenticationSchemeProvider provider = Substitute.For<IAuthenticationSchemeProvider>();
        AuthenticationScheme bearerScheme = new(
            "Bearer",
            displayName: null,
            handlerType: typeof(StubAuthHandler));
        provider.GetAllSchemesAsync().Returns([bearerScheme]);
        return provider;
    }

    private static OpenApiDocumentTransformerContext BuildContext() =>
        new()
        {
            DocumentName = "v1",
            DescriptionGroups = [],
            ApplicationServices = Substitute.For<IServiceProvider>(),
        };

    /// <summary>Minimal IAuthenticationHandler stub required by AuthenticationScheme constructor.</summary>
    private sealed class StubAuthHandler : IAuthenticationHandler
    {
        public Task<AuthenticateResult> AuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context) =>
            Task.CompletedTask;
    }
}
