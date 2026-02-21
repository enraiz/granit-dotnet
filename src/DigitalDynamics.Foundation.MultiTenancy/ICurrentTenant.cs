// =============================================================================
// ICurrentTenant - Accès au tenant courant
// =============================================================================
// Abstraction pour lire et surcharger le contexte tenant dans le flux async.
//
// Implémentation : CurrentTenant (AsyncLocal, dans ce package).
// Usage typique : injecté dans les services qui nécessitent l'isolation tenant.
//
// IDisposable.Change() permet de surcharger temporairement le tenant courant
// (tests, background jobs, workers multi-tenant).
// =============================================================================

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Service pour accéder au tenant courant et surcharger son contexte.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// Indique si un tenant est actif dans le contexte courant.
    /// Vrai uniquement si <see cref="Id"/> est non null.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>Identifiant du tenant courant, ou <c>null</c> si hors contexte tenant.</summary>
    Guid? Id { get; }

    /// <summary>Nom du tenant courant, ou <c>null</c>.</summary>
    string? Name { get; }

    /// <summary>
    /// Surcharge temporairement le tenant courant dans le flux async.
    /// Le tenant précédent est restauré à la libération du scope.
    /// </summary>
    /// <param name="id">Identifiant du tenant à activer, ou <c>null</c> pour désactiver.</param>
    /// <param name="name">Nom optionnel du tenant.</param>
    /// <returns>Scope à libérer pour restaurer le tenant précédent.</returns>
    IDisposable Change(Guid? id, string? name = null);
}
