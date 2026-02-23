namespace Granit.Core.Domain;

/// <summary>
/// Entité avec audit trail complet incluant la suppression logique (RGPD).
/// Hérite de <see cref="AuditedEntity"/> et implémente <see cref="ISoftDeletable"/>.
/// </summary>
public abstract class FullAuditedEntity : AuditedEntity, ISoftDeletable
{
    /// <summary>Indique si l'entité est supprimée logiquement.</summary>
    public virtual bool IsDeleted { get; set; }

    /// <summary>Date de suppression logique (UTC).</summary>
    public virtual DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Identifiant de l'utilisateur ayant supprimé l'entité.</summary>
    public virtual string? DeletedBy { get; set; }
}
