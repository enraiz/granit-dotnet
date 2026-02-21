// =============================================================================
// ITransitEncryptionService - Chiffrement/dechiffrement via Vault Transit
// =============================================================================
// Abstraction pour le chiffrement des donnees FHIR sensibles.
// Implemente dans ce meme package via l'engine Transit (cle fhir-data).
//
// Conformite HDS : les donnees de sante DOIVENT etre chiffrees au repos.
// =============================================================================

namespace DigitalDynamics.Foundation.Vault;

/// <summary>
/// Service de chiffrement/dechiffrement via Vault Transit Engine.
/// Utilise pour proteger les donnees FHIR sensibles au repos.
/// </summary>
public interface ITransitEncryptionService
{
    /// <summary>
    /// Chiffre un texte en clair via Vault Transit.
    /// </summary>
    /// <param name="keyName">Nom de la cle Transit (ex: "fhir-data").</param>
    /// <param name="plaintext">Texte en clair a chiffrer.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    /// <returns>Texte chiffre (format vault:v1:...).</returns>
    Task<string> EncryptAsync(string keyName, string plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dechiffre un texte chiffre via Vault Transit.
    /// </summary>
    /// <param name="keyName">Nom de la cle Transit (ex: "fhir-data").</param>
    /// <param name="ciphertext">Texte chiffre (format vault:v1:...).</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    /// <returns>Texte en clair.</returns>
    Task<string> DecryptAsync(string keyName, string ciphertext, CancellationToken cancellationToken = default);
}
