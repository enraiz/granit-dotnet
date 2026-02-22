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
//     "RequireHttpsMetadata": true,
//     "AdminRole": "admin",
//     "RoleClaimsSource": "realm_access"
//   }
// =============================================================================

namespace DigitalDynamics.Foundation.Security.Keycloak.Options;

/// <summary>
/// Options de configuration pour l'authentification Keycloak OIDC.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>Clé de section dans la configuration.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>URL de l'authority OIDC (ex: https://keycloak.meyers.cloud/realms/guava-health).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Client ID Keycloak (ex: guava-backend).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret (confidentiel — charger depuis Vault, jamais en clair).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Exiger HTTPS pour les métadonnées OIDC. Défaut : true en production.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Audience attendue dans le token. Défaut : ClientId.</summary>
    public string? Audience { get; set; }

    /// <summary>Rôle admin (realm role Keycloak). Défaut : "admin".</summary>
    public string AdminRole { get; set; } = "admin";

    /// <summary>
    /// Source des rôles dans le token Keycloak.
    /// <list type="bullet">
    /// <item><c>"realm_access"</c> (défaut) : rôles realm (<c>realm_access.roles</c>)</item>
    /// <item><c>"resource_access"</c> : rôles client (<c>resource_access.{ClientId}.roles</c>)</item>
    /// </list>
    /// </summary>
    public string RoleClaimsSource { get; set; } = "realm_access";
}
