// =============================================================================
// VaultClientFactory - VaultSharp client creation with authentication
// =============================================================================
// Supports two authentication methods:
//   - Kubernetes (production): uses the ServiceAccount token
//   - Token (development): uses a static token
//
// In production, Kubernetes authentication is REQUIRED.
// =============================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigitalDynamics.Foundation.Vault.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

namespace DigitalDynamics.Foundation.Vault.Services;

/// <summary>
/// Factory for creating a VaultSharp client with the configured authentication method.
/// </summary>
public sealed class VaultClientFactory
{
    private readonly VaultOptions _options;
    private readonly ILogger<VaultClientFactory> _logger;

    public VaultClientFactory(IOptions<VaultOptions> options, ILogger<VaultClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Creates an authenticated VaultSharp client.</summary>
    public IVaultClient Create()
    {
        IAuthMethodInfo authMethod = _options.AuthMethod.ToLowerInvariant() switch
        {
            "kubernetes" => CreateKubernetesAuth(),
            "token" => CreateTokenAuth(),
            _ => throw new InvalidOperationException(
                $"Unknown Vault authentication method: '{_options.AuthMethod}'. " +
                "Allowed values: 'Kubernetes', 'Token'.")
        };

        var settings = new VaultClientSettings(_options.Address, authMethod);
        _logger.LogInformation(
            "Vault client created with method {AuthMethod} targeting {Address}",
            _options.AuthMethod,
            _options.Address);

        return new VaultClient(settings);
    }

    private KubernetesAuthMethodInfo CreateKubernetesAuth()
    {
        var jwt = File.ReadAllText(_options.KubernetesTokenPath);
        _logger.LogDebug("Kubernetes authentication with role {Role}", _options.KubernetesRole);
        return new KubernetesAuthMethodInfo(_options.KubernetesRole, jwt);
    }

    private TokenAuthMethodInfo CreateTokenAuth()
    {
        if (string.IsNullOrEmpty(_options.Token))
        {
            throw new InvalidOperationException(
                "Vault token is required for token authentication. " +
                "Set Vault:Token in the configuration.");
        }

        _logger.LogWarning(
            "Vault static token authentication — use only for local development");
        return new TokenAuthMethodInfo(_options.Token);
    }
}
