using Granit.DataImport.Internal;
using Granit.DataImport.Mapping;
using Granit.DataImport.Pipeline;
using Granit.DataImport.Tests.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitDataImport_registers_semantic_mapping_service()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataImport();

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        ISemanticMappingService service = provider.GetRequiredService<ISemanticMappingService>();
        service.ShouldBeOfType<NullSemanticMappingService>();
    }

    [Fact]
    public void AddGranitDataImport_registers_mapping_suggestion_service()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataImport();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IMappingSuggestionService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitDataImport_registers_import_orchestrator()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataImport();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IImportOrchestrator) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitDataImport_returns_service_collection_for_chaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddGranitDataImport();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddImportDefinition_registers_definition_as_singleton()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddImportDefinition<TestPatient, TestPatientImportDefinition>();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ImportDefinition<TestPatient>) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSemanticMappingService_replaces_default_implementation()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddGranitDataImport();

        // Act
        services.AddSemanticMappingService<FakeSemanticMappingService>();

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        ISemanticMappingService service = provider.GetRequiredService<ISemanticMappingService>();
        service.ShouldBeOfType<FakeSemanticMappingService>();
    }

    [Fact]
    public void AddGranitDataImport_does_not_replace_existing_semantic_service()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<ISemanticMappingService, FakeSemanticMappingService>();

        // Act
        services.AddGranitDataImport();

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        ISemanticMappingService service = provider.GetRequiredService<ISemanticMappingService>();
        service.ShouldBeOfType<FakeSemanticMappingService>();
    }

    private sealed class FakeSemanticMappingService : ISemanticMappingService
    {
        public bool IsAvailable => true;

        public Task<IReadOnlyList<SemanticMappingSuggestion>> SuggestSemanticMappingsAsync(
            IReadOnlyList<string> unmappedHeaders,
            IReadOnlyList<FieldMetadata> targetFields,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SemanticMappingSuggestion>>([]);
    }
}
