// =============================================================================
// FoundationEncryptionModule - Module Foundation pour le chiffrement de chaînes
// =============================================================================
// Configure IStringEncryptionService avec AesStringEncryptionProvider par défaut.
// Pour activer le provider Vault Transit, enregistrer VaultStringEncryptionProvider
// depuis Foundation.Vault et définir Encryption:ProviderName = "Vault".
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Encryption.Extensions;

namespace DigitalDynamics.Foundation.Encryption;

/// <summary>
/// Module Foundation pour le chiffrement de chaînes.
/// Provider par défaut : AES-256-CBC (clé dérivée via PBKDF2 depuis Vault config).
/// </summary>
public sealed class FoundationEncryptionModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationEncryption(
            context.Configuration.GetSection(StringEncryptionOptions.SectionName));
}
