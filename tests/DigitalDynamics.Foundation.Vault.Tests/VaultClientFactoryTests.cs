// =============================================================================
// Tests - VaultClientFactory
// =============================================================================
// Vérifie la création du client Vault avec les différentes méthodes d'auth.
// =============================================================================

using DigitalDynamics.Foundation.Vault.Options;
using DigitalDynamics.Foundation.Vault.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VaultSharp;
using Xunit;

namespace DigitalDynamics.Foundation.Vault.Tests;

public sealed class VaultClientFactoryTests
{
    [Fact]
    public void Create_WithTokenAuth_ReturnsClient()
    {
        // Arrange
        IOptions<VaultOptions> options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Token",
            Token = "dev-token-123"
        });
        VaultClientFactory factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance);

        // Act
        IVaultClient client = factory.Create();

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithTokenAuth_WithoutToken_Throws()
    {
        // Arrange
        IOptions<VaultOptions> options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Token",
            Token = null
        });
        VaultClientFactory factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance);

        // Act & Assert
        Action act = () => factory.Create();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*token*");
    }

    [Fact]
    public void Create_WithUnknownAuthMethod_Throws()
    {
        // Arrange
        IOptions<VaultOptions> options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Unknown"
        });
        VaultClientFactory factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance);

        // Act & Assert
        Action act = () => factory.Create();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*");
    }
}
