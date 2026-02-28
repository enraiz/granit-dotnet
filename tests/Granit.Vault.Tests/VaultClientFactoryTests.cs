// =============================================================================
// Tests - VaultClientFactory
// =============================================================================
// Verifies the creation of the Vault client with different auth methods.
// =============================================================================

using Granit.Vault.Exceptions;
using Granit.Vault.Options;
using Granit.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using VaultSharp;
using Xunit;

namespace Granit.Vault.Tests;

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
        VaultClientFactory factory = new(options, NullLogger<VaultClientFactory>.Instance);

        // Act
        IVaultClient client = factory.Create();

        // Assert
        client.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithTokenAuth_WithoutToken_ThrowsVaultConfigurationException()
    {
        // Arrange
        IOptions<VaultOptions> options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Token",
            Token = null
        });
        VaultClientFactory factory = new(options, NullLogger<VaultClientFactory>.Instance);

        // Act & Assert
        Action act = () => factory.Create();
        Should.Throw<VaultConfigurationException>(act)
            .ErrorCode.ShouldBe("Vault:TokenRequired");
    }

    [Fact]
    public void Create_WithUnknownAuthMethod_ThrowsVaultConfigurationException()
    {
        // Arrange
        IOptions<VaultOptions> options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Unknown"
        });
        VaultClientFactory factory = new(options, NullLogger<VaultClientFactory>.Instance);

        // Act & Assert
        Action act = () => factory.Create();
        Should.Throw<VaultConfigurationException>(act)
            .ErrorCode.ShouldBe("Vault:UnknownAuthMethod");
    }
}
