// =============================================================================
// Tests - VaultClientFactory
// =============================================================================
// Verifies the creation of the Vault client with different auth methods.
// =============================================================================

using FluentAssertions;
using Granit.Localization;
using Granit.Localization.Extensions;
using Granit.Vault.Options;
using Granit.Vault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VaultSharp;
using Xunit;

namespace Granit.Vault.Tests;

public sealed class VaultClientFactoryTests
{
    private static IStringLocalizer<VaultLocalizationResource> CreateLocalizer()
    {
        ServiceCollection services = new();
        services.AddGranitLocalization(options =>
        {
            options.Resources
                .Add<VaultLocalizationResource>(defaultCulture: "fr")
                .AddJson(
                    typeof(VaultLocalizationResource).Assembly,
                    "Granit.Vault.Localization.Vault")
                .AddBaseTypes(typeof(GranitLocalizationResource));
        });
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IStringLocalizer<VaultLocalizationResource>>();
    }

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
        var factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance, CreateLocalizer());

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
        var factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance, CreateLocalizer());

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
        var factory = new VaultClientFactory(options, NullLogger<VaultClientFactory>.Instance, CreateLocalizer());

        // Act & Assert
        Action act = () => factory.Create();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*");
    }
}
