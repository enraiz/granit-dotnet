// =============================================================================
// StringEncryptionOptions - Configuration du service de chiffrement
// =============================================================================
// Chargées depuis la section "Encryption" de appsettings.json.
// La PassPhrase DOIT être fournie via Vault (jamais hardcodée).
//
// Note HDS (CWE-329) : aucun Salt ni InitVector statique.
// L'IV est généré aléatoirement à chaque chiffrement par le provider AES.
// =============================================================================

namespace DigitalDynamics.Foundation.Encryption;

/// <summary>
/// Options de configuration du service de chiffrement de chaînes.
/// </summary>
public sealed class StringEncryptionOptions
{
    /// <summary>Nom de la section de configuration.</summary>
    public const string SectionName = "Encryption";

    /// <summary>Nom du provider AES local.</summary>
    public const string AesProviderName = "Aes";

    /// <summary>Nom du provider Vault Transit.</summary>
    public const string VaultProviderName = "Vault";

    /// <summary>
    /// Phrase secrète utilisée par le provider AES pour dériver la clé de chiffrement.
    /// DOIT être fournie via Vault config provider — jamais hardcodée ni committée.
    /// </summary>
    public string PassPhrase { get; set; } = string.Empty;

    /// <summary>Taille de la clé AES en bits (256 par défaut = AES-256).</summary>
    public int KeySize { get; set; } = 256;

    /// <summary>
    /// Nom du provider actif. Valeurs : "Aes" (défaut) ou "Vault".
    /// </summary>
    public string ProviderName { get; set; } = AesProviderName;

    /// <summary>
    /// Nom de la clé Transit Vault utilisée par VaultStringEncryptionProvider.
    /// Ignoré si ProviderName != "Vault".
    /// </summary>
    public string VaultKeyName { get; set; } = "string-encryption";
}
