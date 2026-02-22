// =============================================================================
// ICurrentUserService - Accès à l'utilisateur courant
// =============================================================================
// Abstraction pour récupérer l'identité de l'utilisateur authentifié.
// Implémenté dans Foundation.Authentication.JwtBearer (CurrentUserService via HttpContext).
// =============================================================================

namespace DigitalDynamics.Foundation.Security;

/// <summary>
/// Service pour accéder aux informations de l'utilisateur courant.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Identifiant unique de l'utilisateur (claim "sub").</summary>
    string? UserId { get; }

    /// <summary>Nom d'utilisateur (selon <c>NameClaimType</c> configuré dans JWT Bearer).</summary>
    string? UserName { get; }

    /// <summary>Email de l'utilisateur.</summary>
    string? Email { get; }

    /// <summary>Indique si l'utilisateur est authentifié.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Rôles de l'utilisateur.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Vérifie si l'utilisateur possède un rôle donné.</summary>
    bool IsInRole(string role);
}
