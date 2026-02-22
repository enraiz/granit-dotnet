// =============================================================================
// IStringEncryptionService - Chiffrement/déchiffrement de chaînes
// =============================================================================
// Abstraction principale pour le chiffrement de chaînes.
// Délègue au provider sélectionné via StringEncryptionOptions.ProviderName.
//
// Implémentation : DefaultStringEncryptionService (dans ce package).
// Providers disponibles : AES-256-CBC (local) ou Vault Transit (distant).
// =============================================================================

namespace DigitalDynamics.Foundation.Encryption;

/// <summary>
/// Service de chiffrement/déchiffrement de chaînes.
/// </summary>
public interface IStringEncryptionService
{
    /// <summary>
    /// Chiffre une chaîne en clair.
    /// </summary>
    /// <param name="plainText">Texte en clair à chiffrer.</param>
    /// <returns>Texte chiffré encodé en Base64.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Déchiffre une chaîne chiffrée.
    /// </summary>
    /// <param name="cipherText">Texte chiffré encodé en Base64.</param>
    /// <returns>Texte en clair, ou <c>null</c> si le déchiffrement échoue.</returns>
    string? Decrypt(string cipherText);
}
