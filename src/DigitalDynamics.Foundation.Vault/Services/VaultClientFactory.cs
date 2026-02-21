// =============================================================================
// VaultClientFactory - Création du client VaultSharp avec authentification
// =============================================================================
// Supporte deux méthodes d'authentification :
//   - Kubernetes (production) : utilise le ServiceAccount token
//   - Token (développement) : utilise un token statique
//
// En production, l'authentification Kubernetes est OBLIGATOIRE.
// =============================================================================

using DigitalDynamics.Foundation.Vault.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

namespace DigitalDynamics.Foundation.Vault.Services;

/// <summary>
/// Factory pour créer un client VaultSharp avec la méthode d'authentification configurée.
/// </summary>
public sealed partial class VaultClientFactory
{
    private readonly VaultOptions _options;
    private readonly ILogger<VaultClientFactory> _logger;

    public VaultClientFactory(IOptions<VaultOptions> options, ILogger<VaultClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Crée un client VaultSharp authentifié.</summary>
    public IVaultClient Create()
    {
        IAuthMethodInfo authMethod = _options.AuthMethod.ToLowerInvariant() switch
        {
            "kubernetes" => CreateKubernetesAuth(),
            "token" => CreateTokenAuth(),
            _ => throw new InvalidOperationException(
                $"Méthode d'authentification Vault inconnue : '{_options.AuthMethod}'. " +
                "Valeurs autorisées : 'Kubernetes', 'Token'.")
        };

        var settings = new VaultClientSettings(_options.Address, authMethod);
        LogClientCreated(_logger, _options.AuthMethod, _options.Address);

        return new VaultClient(settings);
    }

    private KubernetesAuthMethodInfo CreateKubernetesAuth()
    {
        var jwt = File.ReadAllText(_options.KubernetesTokenPath);
        LogKubernetesAuth(_logger, _options.KubernetesRole);
        return new KubernetesAuthMethodInfo(_options.KubernetesRole, jwt);
    }

    private TokenAuthMethodInfo CreateTokenAuth()
    {
        if (string.IsNullOrEmpty(_options.Token))
        {
            throw new InvalidOperationException(
                "Le token Vault est requis pour l'authentification par token. " +
                "Configurez Vault:Token dans la configuration.");
        }

        LogTokenAuth(_logger);
        return new TokenAuthMethodInfo(_options.Token);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Client Vault créé avec la méthode {AuthMethod} vers {Address}")]
    private static partial void LogClientCreated(ILogger logger, string authMethod, string address);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Authentification Kubernetes avec le rôle {Role}")]
    private static partial void LogKubernetesAuth(ILogger logger, string role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authentification Vault par token statique — utiliser uniquement en développement local")]
    private static partial void LogTokenAuth(ILogger logger);
}
