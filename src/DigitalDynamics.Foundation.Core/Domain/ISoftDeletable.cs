// =============================================================================
// ISoftDeletable - Interface de suppression logique (RGPD)
// =============================================================================
// Les entites contenant des donnees personnelles implementent cette interface
// pour supporter le droit a l'oubli RGPD via suppression logique.
//
// Le SoftDeleteInterceptor (package Persistence) intercepte les DELETE
// et les transforme en UPDATE SET IsDeleted = true.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Interface pour la suppression logique RGPD.
/// Les entites marquees ne sont plus retournees par les requetes standard
/// mais restent en base pour l'audit trail HDS.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Indique si l'entite est supprimee logiquement.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Date de suppression logique (UTC).</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Identifiant de l'utilisateur ayant supprime l'entite.</summary>
    string? DeletedBy { get; set; }
}
