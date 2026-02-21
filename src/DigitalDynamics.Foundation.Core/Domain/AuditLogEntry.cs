// =============================================================================
// AuditLogEntry - Entree du journal d'audit HDS
// =============================================================================
// Chaque modification d'entite genere une entree d'audit pour satisfaire
// l'exigence HDS de tracabilite sur 3 ans.
//
// Stocke dans un schema dedie "audit" avec retention configuree.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Entree d'audit trail pour la conformite HDS.
/// Enregistre qui a fait quoi, quand, sur quelle entite.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>Identifiant unique de l'entree d'audit.</summary>
    public Guid Id { get; init; }

    /// <summary>Horodatage de l'operation (UTC).</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Identifiant de l'utilisateur ayant effectue l'operation.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Type d'operation : Create, Update, Delete, SoftDelete.</summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>Type CLR de l'entite concernee.</summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>Identifiant de l'entite concernee.</summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>Proprietes modifiees, serialisees en JSON (sans donnees sensibles).</summary>
    public string? Changes { get; init; }

    /// <summary>Adresse IP source de la requete.</summary>
    public string? IpAddress { get; init; }

    /// <summary>User-Agent du client.</summary>
    public string? UserAgent { get; init; }
}
