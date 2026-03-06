// =============================================================================
// Tests - NullableIntSchemaOperationTransformer
// =============================================================================
// Vérifie que les paramètres query nullable int (type: [integer, string])
// sont normalisés en type: [integer, null].
// =============================================================================

using Granit.ApiDocumentation.Transformers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.ApiDocumentation.Tests;

public sealed class NullableIntSchemaOperationTransformerTests
{
    // --- int? query param (integer|string, int32) → integer|null ---

    [Fact]
    public async Task TransformAsync_NullableInt32_NormalizesToIntegerNull()
    {
        // Arrange
        NullableIntSchemaOperationTransformer transformer = new();
        OpenApiOperation operation = BuildOperation(
            JsonSchemaType.Integer | JsonSchemaType.String, "int32");

        // Act
        await transformer.TransformAsync(operation, BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters![0].Schema!.Type.ShouldBe(JsonSchemaType.Integer | JsonSchemaType.Null);
        operation.Parameters[0].Schema!.Format.ShouldBe("int32");
    }

    // --- long? query param (integer|string, int64) → integer|null ---

    [Fact]
    public async Task TransformAsync_NullableInt64_NormalizesToIntegerNull()
    {
        // Arrange
        NullableIntSchemaOperationTransformer transformer = new();
        OpenApiOperation operation = BuildOperation(
            JsonSchemaType.Integer | JsonSchemaType.String, "int64");

        // Act
        await transformer.TransformAsync(operation, BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters![0].Schema!.Type.ShouldBe(JsonSchemaType.Integer | JsonSchemaType.Null);
        operation.Parameters[0].Schema!.Format.ShouldBe("int64");
    }

    // --- Regular integer param → unchanged ---

    [Fact]
    public async Task TransformAsync_PlainInteger_Unchanged()
    {
        // Arrange
        NullableIntSchemaOperationTransformer transformer = new();
        OpenApiOperation operation = BuildOperation(JsonSchemaType.Integer, "int32");

        // Act
        await transformer.TransformAsync(operation, BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters![0].Schema!.Type.ShouldBe(JsonSchemaType.Integer);
    }

    // --- String param → unchanged ---

    [Fact]
    public async Task TransformAsync_StringParam_Unchanged()
    {
        // Arrange
        NullableIntSchemaOperationTransformer transformer = new();
        OpenApiOperation operation = BuildOperation(JsonSchemaType.String, null);

        // Act
        await transformer.TransformAsync(operation, BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters![0].Schema!.Type.ShouldBe(JsonSchemaType.String);
    }

    // --- Null parameters → no crash ---

    [Fact]
    public async Task TransformAsync_NullParameters_DoesNotThrow()
    {
        // Arrange
        NullableIntSchemaOperationTransformer transformer = new();
        OpenApiOperation operation = new() { Parameters = null };

        // Act & Assert
        await Should.NotThrowAsync(() =>
            transformer.TransformAsync(operation, BuildContext(), TestContext.Current.CancellationToken));
    }

    // --- Helpers ---

    private static OpenApiOperation BuildOperation(JsonSchemaType type, string? format) =>
        new()
        {
            Parameters =
            [
                new OpenApiParameter
                {
                    Name = "skip",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = type, Format = format },
                },
            ],
        };

    private static OpenApiOperationTransformerContext BuildContext()
    {
        ActionDescriptor descriptor = new();
        return new OpenApiOperationTransformerContext
        {
            DocumentName = "v1",
            Description = new ApiDescription
            {
                ActionDescriptor = descriptor,
                RelativePath = "api/test",
            },
            ApplicationServices = Substitute.For<IServiceProvider>(),
        };
    }
}
