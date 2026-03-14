using Granit.Vault.HashiCorp.Exceptions;
using Granit.Vault.HashiCorp.Options;
using Granit.Vault.HashiCorp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using VaultSharp;
using Xunit;

namespace Granit.Vault.HashiCorp.Tests;

public sealed class VaultClientFactoryTests
{
    [Fact]
    public void Create_WithTokenAuth_ReturnsClient()
    {
        IOptions<HashiCorpVaultOptions> options = Microsoft.Extensions.Options.Options.Create(new HashiCorpVaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Token",
            Token = "dev-token-123"
        });
        VaultClientFactory factory = new(options, NullLogger<VaultClientFactory>.Instance);

        IVaultClient client = factory.Create();

        client.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithTokenAuth_WithoutToken_ThrowsConfigurationException()
    {
        IOptions<HashiCorpVaultOptions> options = Microsoft.Extensions.Options.Options.Create(new HashiCorpVaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Token",
            Token = null
        });
        VaultClientFactory factory = new(options, NullLogger<VaultClientFactory>.Instance);

        Action act = () => factory.Create();
        Should.Throw<HashiCorpVaultConfigurationException>(act)
            .ErrorCode.ShouldBe("Vault:TokenRequired");
    }

    [Fact]
    public void Create_WithUnknownAuthMethod_ThrowsConfigurationException()
    {
        IOptions<HashiCorpVaultOptions> options = Microsoft.Extensions.Options.Options.Create(new HashiCorpVaultOptions
        {
            Address = "http://localhost:8200",
            AuthMethod = "Unknown"
        });
        VaultClientFactory factory = new(options, NullLogger<VaultClientFactory>.Instance);

        Action act = () => factory.Create();
        Should.Throw<HashiCorpVaultConfigurationException>(act)
            .ErrorCode.ShouldBe("Vault:UnknownAuthMethod");
    }
}
