// =============================================================================
// Tests - TenantHeaderOperationTransformer
// =============================================================================
// Vérifie que le header X-Tenant-Id est ajouté conditionnellement aux opérations
// OpenAPI en fonction de EnableTenantHeader et [AllowAnonymousTenant].
// =============================================================================

using FluentAssertions;
using Granit.ApiDocumentation.Options;
using Granit.ApiDocumentation.Transformers;
using Granit.Core.MultiTenancy;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using NSubstitute;
using Xunit;

namespace Granit.ApiDocumentation.Tests;

public sealed class TenantHeaderOperationTransformerTests
{
    // --- EnableTenantHeader = false → no header added ---

    [Fact]
    public async Task TransformAsync_TenantHeaderDisabled_NoParameterAdded()
    {
        // Arrange
        TenantHeaderOperationTransformer transformer = BuildTransformer(enableTenantHeader: false);
        OpenApiOperation operation = new();
        OpenApiOperationTransformerContext context = BuildContext();

        // Act
        await transformer.TransformAsync(operation, context, TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters.Should().BeNullOrEmpty();
    }

    // --- EnableTenantHeader = true, no [AllowAnonymousTenant] → header added ---

    [Fact]
    public async Task TransformAsync_TenantHeaderEnabled_HeaderAddedAsRequired()
    {
        // Arrange
        TenantHeaderOperationTransformer transformer = BuildTransformer(enableTenantHeader: true);
        OpenApiOperation operation = new();
        OpenApiOperationTransformerContext context = BuildContext();

        // Act
        await transformer.TransformAsync(operation, context, TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters.Should().ContainSingle()
            .Which.Name.Should().Be("X-Tenant-Id");
        operation.Parameters![0].In.Should().Be(ParameterLocation.Header);
        operation.Parameters[0].Required.Should().BeTrue();
    }

    // --- EnableTenantHeader = true, with [AllowAnonymousTenant] → no header ---

    [Fact]
    public async Task TransformAsync_TenantHeaderEnabledWithAllowAnonymousTenant_NoParameterAdded()
    {
        // Arrange
        TenantHeaderOperationTransformer transformer = BuildTransformer(enableTenantHeader: true);
        OpenApiOperation operation = new();
        OpenApiOperationTransformerContext context = BuildContext(new AllowAnonymousTenantAttribute());

        // Act
        await transformer.TransformAsync(operation, context, TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters.Should().BeNullOrEmpty();
    }

    // --- Custom header name ---

    [Fact]
    public async Task TransformAsync_CustomHeaderName_UsesConfiguredName()
    {
        // Arrange
        TenantHeaderOperationTransformer transformer = BuildTransformer(
            enableTenantHeader: true,
            tenantHeaderName: "X-Organization-Id");
        OpenApiOperation operation = new();
        OpenApiOperationTransformerContext context = BuildContext();

        // Act
        await transformer.TransformAsync(operation, context, TestContext.Current.CancellationToken);

        // Assert
        operation.Parameters.Should().ContainSingle()
            .Which.Name.Should().Be("X-Organization-Id");
    }

    // --- Helpers ---

    private static TenantHeaderOperationTransformer BuildTransformer(
        bool enableTenantHeader,
        string tenantHeaderName = "X-Tenant-Id")
    {
        ApiDocumentationOptions options = new()
        {
            EnableTenantHeader = enableTenantHeader,
            TenantHeaderName = tenantHeaderName,
        };
        return new TenantHeaderOperationTransformer(Microsoft.Extensions.Options.Options.Create(options));
    }

    private static OpenApiOperationTransformerContext BuildContext(params object[] metadata)
    {
        ActionDescriptor descriptor = new();
        descriptor.EndpointMetadata = [.. metadata];

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
