using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Encryption.Extensions;

namespace DigitalDynamics.Foundation.Encryption;

/// <summary>
/// Foundation module for string encryption.
/// Default provider: AES-256-CBC (key derived via PBKDF2 from Vault config).
/// </summary>
public sealed class FoundationEncryptionModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationEncryption(
            context.Configuration.GetSection(StringEncryptionOptions.SectionName));
}
