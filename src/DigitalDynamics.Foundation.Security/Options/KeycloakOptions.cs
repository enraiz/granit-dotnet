// =============================================================================
// KeycloakOptions - Configuration de l'authentification Keycloak OIDC
// =============================================================================
// Bind depuis la section "Keycloak" de la configuration (IOptions<T> pattern).
//
// Exemple appsettings.json :
//   "Keycloak": {
//     "Authority": "https://keycloak.meyers.cloud/realms/guava-health",
//     "ClientId": "guava-backend",
//     "ClientSecret": "***",
//     "RequireHttpsMetadata": true
//   }
// =============================================================================

namespace DigitalDynamics.Foundation.Security.Options;

/// <summary>
/// Options de configuration pour l'authentification Keycloak OIDC.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>Clé de section dans la configuration.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>URL de l'authority OIDC (ex: https://keycloak.meyers.cloud/realms/guava-health).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Client ID (ex: guava-backend).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret (confidentiel).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Exiger HTTPS pour les métadonnées OIDC. Défaut : true en production.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Audience attendue dans le token. Défaut : ClientId.</summary>
    public string? Audience { get; set; }

    /// <summary>Rôle admin (realm role). Défaut : "admin".</summary>
    public string AdminRole { get; set; } = "admin";
}
