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
