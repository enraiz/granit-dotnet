// =============================================================================
// Tests - VaultClientFactory
// =============================================================================
// Verifies the creation of the Vault client with different auth methods.
// =============================================================================

using DigitalDynamics.Foundation.Vault.Options;
using DigitalDynamics.Foundation.Vault.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Vault.Tests;

public sealed class VaultClientFactoryTests
{
    [Fact]
    public void Create_WithTokenAuth_ReturnsClient()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Token",
            Token = "dev-token-123"
        });
        var factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance);

        // Act
        var client = factory.Create();

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithTokenAuth_WithoutToken_Throws()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Token",
            Token = null
        });
        var factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance);

        // Act & Assert
        var act = () => factory.Create();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*token*");
    }

    [Fact]
    public void Create_WithUnknownAuthMethod_Throws()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Unknown"
        });
        var factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance);

        // Act & Assert
        var act = () => factory.Create();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*");
    }
}
