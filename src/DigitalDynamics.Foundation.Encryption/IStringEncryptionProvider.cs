// =============================================================================
// IStringEncryptionProvider - Provider de chiffrement de chaînes
// =============================================================================
// Contrat pour les implémentations concrètes de chiffrement.
// Les providers sont des singletons auto-configurés via DI.
//
// Providers disponibles :
//   - AesStringEncryptionProvider : AES-256-CBC local (< 1 ms)
//   - VaultStringEncryptionProvider : Vault Transit Engine (10-20 ms)
// =============================================================================

namespace DigitalDynamics.Foundation.Encryption;

/// <summary>
/// Provider de chiffrement/déchiffrement de chaînes.
/// Chaque implémentation gère sa propre configuration via DI.
/// </summary>
public interface IStringEncryptionProvider
{
    /// <summary>Nom du provider (ex: "Aes", "Vault").</summary>
    string ProviderName { get; }

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
