// =============================================================================
// JwtBearerAuthOptions - Configuration JWT Bearer générique (OIDC)
// =============================================================================
// Bind depuis la section "Authentication" de la configuration (IOptions<T> pattern).
//
// Exemple appsettings.json :
//   "Authentication": {
//     "Authority": "https://idp.example.com/realms/myrealm",
//     "Audience": "my-client",
//     "RequireHttpsMetadata": true,
//     "NameClaimType": "sub"
//   }
//
// Pour Keycloak : utiliser FoundationSecurityKeycloakModule
// (lit la section "Keycloak", pas de section "Authentication" nécessaire).
// =============================================================================

namespace DigitalDynamics.Foundation.Security.Options;

/// <summary>
/// Options de configuration pour l'authentification JWT Bearer générique (OIDC-compatible).
/// </summary>
public sealed class JwtBearerAuthOptions
{
    /// <summary>Clé de section dans la configuration.</summary>
    public const string SectionName = "Authentication";

    /// <summary>URL de l'authority OIDC (ex: https://idp.example.com/realms/myrealm).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Audience attendue dans le token.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Exiger HTTPS pour les métadonnées OIDC. Défaut : true en production.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Claim utilisé comme nom d'utilisateur (<see cref="System.Security.Principal.IIdentity.Name"/>).
    /// Défaut : <c>"sub"</c> (RFC 7519 — claim obligatoire, toujours présent dans un JWT valide).
    /// </summary>
    public string NameClaimType { get; set; } = "sub";
}
