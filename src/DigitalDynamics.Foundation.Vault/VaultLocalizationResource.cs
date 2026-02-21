// ---------------------------------------------------------------------------
// VaultLocalizationResource.cs
// Classe marker de la ressource de localisation du module Vault.
// Hérite de FoundationLocalizationResource pour les messages communs.
// ---------------------------------------------------------------------------

using DigitalDynamics.Foundation.Localization;
using DigitalDynamics.Foundation.Localization.Attributes;

namespace DigitalDynamics.Foundation.Vault;

/// <summary>
/// Ressource de localisation du module Vault.
/// </summary>
[LocalizationResourceName("Vault")]
[InheritResource(typeof(FoundationLocalizationResource))]
public sealed class VaultLocalizationResource;
