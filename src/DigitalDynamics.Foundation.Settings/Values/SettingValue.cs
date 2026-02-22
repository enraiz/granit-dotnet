// =============================================================================
// SettingValue - Valeur d'un paramètre pour un provider et une clé donnés
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Values;

/// <summary>
/// Représente la valeur d'un paramètre pour un provider et une clé donnés.
/// </summary>
/// <param name="Name">Nom du paramètre.</param>
/// <param name="ProviderName">Nom du provider (ex : "G", "T", "U", "C", "D").</param>
/// <param name="ProviderKey">Clé du provider (null = Global, tenantId = Tenant, userId = User).</param>
/// <param name="Value">Valeur du paramètre (plaintext — le chiffrement est géré par ISettingStore).</param>
public sealed record SettingValue(
    string Name,
    string ProviderName,
    string? ProviderKey,
    string? Value);
