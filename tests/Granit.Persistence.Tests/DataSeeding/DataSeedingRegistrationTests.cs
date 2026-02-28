// =============================================================================
// Tests — AddGranitDataSeeding
// =============================================================================
// Vérifie que l'extension d'enregistrement DI :
//   - Enregistre IDataSeeder comme Singleton
//   - Enregistre le DataSeedingHostedService comme IHostedService
//   - N'enregistre PAS de IDataSeedContributor (responsabilité des modules)
// =============================================================================

using FluentAssertions;
using Granit.Persistence.DataSeeding;
using Granit.Persistence.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Persistence.Tests.DataSeeding;

public sealed class DataSeedingRegistrationTests
{
    [Fact]
    public void AddGranitDataSeeding_RegistersDataSeeder_AsSingleton()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataSeeding();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDataSeeder));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<DataSeeder>();
    }

    [Fact]
    public void AddGranitDataSeeding_RegistersHostedService()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataSeeding();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IHostedService)
                 && d.ImplementationType == typeof(DataSeedingHostedService));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitDataSeeding_DoesNotRegisterContributors()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataSeeding();

        // Assert
        services.Where(d => d.ServiceType == typeof(IDataSeedContributor))
            .Should().BeEmpty();
    }

    [Fact]
    public void AddGranitDataSeeding_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddGranitDataSeeding();

        // Assert
        result.Should().BeSameAs(services);
    }
}
