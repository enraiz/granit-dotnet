// =============================================================================
// IGuidGenerator - Abstraction pour la generation d'identifiants GUID
// =============================================================================
// Remplace tout appel direct a Guid.NewGuid() dans le code applicatif.
// L'implementation par defaut (SequentialGuidGenerator) genere des GUID
// sequentiels optimises pour les index clustered de la base de donnees.
//
// Usage : injecter IGuidGenerator dans les handlers, intercepteurs, services.
//   var id = guidGenerator.Create();
//
// Inspire de Volo.Abp.Guids.IGuidGenerator.
// =============================================================================

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Abstraction pour la generation d'identifiants GUID.
/// Remplace <see cref="Guid.NewGuid()"/> pour centraliser la generation
/// et permettre les GUID sequentiels.
/// </summary>
public interface IGuidGenerator
{
    /// <summary>Cree un nouveau <see cref="Guid"/>.</summary>
    Guid Create();
}
