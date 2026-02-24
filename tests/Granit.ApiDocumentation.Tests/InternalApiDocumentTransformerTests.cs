// =============================================================================
// Tests - InternalApiDocumentTransformer
// =============================================================================
// Vérifie que les paths dont le contrôleur ou l'action est marqué [InternalApi]
// sont retirés du document OpenAPI, et que les paths publics sont conservés.
// =============================================================================

using System.Reflection;
using FluentAssertions;
using Granit.ApiDocumentation.Attributes;
using Granit.ApiDocumentation.Transformers;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NSubstitute;
using Xunit;

namespace Granit.ApiDocumentation.Tests;

public sealed class InternalApiDocumentTransformerTests
{
    // --- Controller marked [InternalApi] → path removed ---

    [Fact]
    public async Task TransformAsync_InternalController_PathRemoved()
    {
        // Arrange
        InternalApiDocumentTransformer transformer = new();
        OpenApiDocument document = BuildDocument("/api/v1/internal");
        OpenApiDocumentTransformerContext context = BuildContext(
            controllerType: typeof(InternalController),
            actionName: nameof(InternalController.Action),
            relativePath: "api/v1/internal");

        // Act
        await transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        document.Paths.Should().NotContainKey("/api/v1/internal");
    }

    // --- Action marked [InternalApi] → path removed ---

    [Fact]
    public async Task TransformAsync_InternalAction_PathRemoved()
    {
        // Arrange
        InternalApiDocumentTransformer transformer = new();
        OpenApiDocument document = BuildDocument("/api/v1/action");
        OpenApiDocumentTransformerContext context = BuildContext(
            controllerType: typeof(PublicControllerWithInternalAction),
            actionName: nameof(PublicControllerWithInternalAction.InternalAction),
            relativePath: "api/v1/action");

        // Act
        await transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        document.Paths.Should().NotContainKey("/api/v1/action");
    }

    // --- No [InternalApi] → path kept ---

    [Fact]
    public async Task TransformAsync_PublicController_PathKept()
    {
        // Arrange
        InternalApiDocumentTransformer transformer = new();
        OpenApiDocument document = BuildDocument("/api/v1/public");
        OpenApiDocumentTransformerContext context = BuildContext(
            controllerType: typeof(PublicController),
            actionName: nameof(PublicController.Action),
            relativePath: "api/v1/public");

        // Act
        await transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        document.Paths.Should().ContainKey("/api/v1/public");
    }

    // --- Non-ControllerActionDescriptor → path kept ---

    [Fact]
    public async Task TransformAsync_NonControllerDescriptor_PathKept()
    {
        // Arrange
        InternalApiDocumentTransformer transformer = new();
        OpenApiDocument document = BuildDocument("/api/v1/minimal");

        // ApiDescription with a non-ControllerActionDescriptor (e.g. minimal API endpoint)
        Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor nonControllerDescriptor = new();
        ApiDescription apiDesc = new()
        {
            ActionDescriptor = nonControllerDescriptor,
            RelativePath = "api/v1/minimal",
        };

        OpenApiDocumentTransformerContext context = new()
        {
            DocumentName = "v1",
            DescriptionGroups = [new ApiDescriptionGroup("v1", [apiDesc])],
            ApplicationServices = Substitute.For<IServiceProvider>(),
        };

        // Act
        await transformer.TransformAsync(document, context, TestContext.Current.CancellationToken);

        // Assert
        document.Paths.Should().ContainKey("/api/v1/minimal");
    }

    // --- Helpers ---

    private static OpenApiDocument BuildDocument(string path) =>
        new()
        {
            Paths = new OpenApiPaths
            {
                [path] = new OpenApiPathItem(),
            },
        };

    private static OpenApiDocumentTransformerContext BuildContext(
        Type controllerType,
        string actionName,
        string relativePath)
    {
        ControllerActionDescriptor descriptor = new()
        {
            MethodInfo = controllerType.GetMethod(actionName)!,
            ControllerTypeInfo = controllerType.GetTypeInfo(),
        };

        ApiDescription apiDesc = new()
        {
            ActionDescriptor = descriptor,
            RelativePath = relativePath,
        };

        return new OpenApiDocumentTransformerContext
        {
            DocumentName = "v1",
            DescriptionGroups = [new ApiDescriptionGroup("v1", [apiDesc])],
            ApplicationServices = Substitute.For<IServiceProvider>(),
        };
    }

    // --- Fake controllers ---

    [InternalApi]
    private sealed class InternalController
    {
        public static void Action() { }
    }

    private sealed class PublicController
    {
        public static void Action() { }
    }

    private sealed class PublicControllerWithInternalAction
    {
        [InternalApi]
        public static void InternalAction() { }
    }
}
