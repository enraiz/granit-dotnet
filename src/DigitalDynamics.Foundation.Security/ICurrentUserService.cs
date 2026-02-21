// =============================================================================
// ICurrentUserService - Acces a l'utilisateur courant
// =============================================================================
// Abstraction pour recuperer l'identite de l'utilisateur authentifie.
// Implemente via HttpContext dans ce meme package.
// =============================================================================

namespace DigitalDynamics.Foundation.Security;

/// <summary>
/// Service pour acceder aux informations de l'utilisateur courant.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Identifiant unique de l'utilisateur (sub claim Keycloak).</summary>
    string? UserId { get; }

    /// <summary>Nom d'utilisateur (preferred_username claim).</summary>
    string? UserName { get; }

    /// <summary>Email de l'utilisateur.</summary>
    string? Email { get; }

    /// <summary>Indique si l'utilisateur est authentifie.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Roles de l'utilisateur (realm_access.roles de Keycloak).</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Verifie si l'utilisateur possede un role donne.</summary>
    bool IsInRole(string role);
}
