using Granit.Imaging.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Imaging.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitImaging_ReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddGranitImaging();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitImaging_DoesNotRegisterAnyService()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitImaging();

        // Assert — base package has no services, only interfaces
        services.ShouldBeEmpty();
    }
}
