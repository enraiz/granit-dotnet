// =============================================================================
// Tests - GranitPersistenceModule
// =============================================================================
// Vérifie que le module :
//   - Enregistre les intercepteurs EF Core via ConfigureServices
//   - Déclare les dépendances [DependsOn] correctes
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Granit.Guids;
using Granit.MultiTenancy;
using Granit.Persistence.Interceptors;
using Granit.Security;
using Granit.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Persistence.Tests;

public sealed class GranitPersistenceModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersInterceptors()
    {
        // Arrange
        GranitPersistenceModule module = new();
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
        DependsOnAttribute[] attributes = [.. typeof(GranitPersistenceModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()];

        // Assert
        attributes.Should().HaveCount(1);
        Type[] dependedTypes = attributes[0].DependedTypes;
        dependedTypes.Should().Contain(typeof(GranitTimingModule));
        dependedTypes.Should().Contain(typeof(GranitGuidsModule));
        dependedTypes.Should().Contain(typeof(GranitSecurityModule));
        dependedTypes.Should().Contain(typeof(GranitMultiTenancyModule));
    }
}
