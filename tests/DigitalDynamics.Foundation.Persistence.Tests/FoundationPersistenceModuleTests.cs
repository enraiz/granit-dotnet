// =============================================================================
// Tests - FoundationPersistenceModule
// =============================================================================
// Vérifie que le module :
//   - Enregistre les intercepteurs EF Core via ConfigureServices
//   - Déclare les dépendances [DependsOn] correctes
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Guids;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Persistence.Interceptors;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Timing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Persistence.Tests;

public sealed class FoundationPersistenceModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersInterceptors()
    {
        // Arrange
        FoundationPersistenceModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        // Assert
        ServiceDescriptor? auditDescriptor = builder.Services.FirstOrDefault(
            d => d.ServiceType == typeof(AuditedEntityInterceptor));
        auditDescriptor.Should().NotBeNull();

        ServiceDescriptor? softDeleteDescriptor = builder.Services.FirstOrDefault(
            d => d.ServiceType == typeof(SoftDeleteInterceptor));
        softDeleteDescriptor.Should().NotBeNull();
    }

    [Fact]
    public void DependsOn_DeclaresCorrectDependencies()
    {
        // Arrange
        DependsOnAttribute[] attributes = [.. typeof(FoundationPersistenceModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()];

        // Assert
        attributes.Should().HaveCount(1);
        Type[] dependedTypes = attributes[0].DependedTypes;
        dependedTypes.Should().Contain(typeof(FoundationTimingModule));
        dependedTypes.Should().Contain(typeof(FoundationGuidsModule));
        dependedTypes.Should().Contain(typeof(FoundationSecurityModule));
        dependedTypes.Should().Contain(typeof(FoundationMultiTenancyModule));
    }
}
