// =============================================================================
// SimpleGuidGenerator - Wrapper autour de Guid.NewGuid()
// =============================================================================
// Implementation simple de IGuidGenerator pour les cas ou la sequentialite
// n'est pas necessaire : tests unitaires, identifiants temporaires,
// contextes sans injection de dependances.
//
// SimpleGuidGenerator.Instance fournit un acces statique sans DI.
//
// N'est PAS enregistre dans le conteneur DI (SequentialGuidGenerator est
// l'implementation par defaut). Utilisation manuelle uniquement.
//
// Inspire de Volo.Abp.Guids.SimpleGuidGenerator.
// =============================================================================

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Implementation de <see cref="IGuidGenerator"/> basee sur <see cref="Guid.NewGuid()"/>.
/// Disponible via <see cref="Instance"/> pour les contextes sans DI.
/// </summary>
public sealed class SimpleGuidGenerator : IGuidGenerator
{
    /// <summary>
    /// Instance statique pour les contextes sans injection de dependances.
    /// </summary>
    public static SimpleGuidGenerator Instance { get; } = new();

    /// <inheritdoc />
    public Guid Create() => Guid.NewGuid();
}
