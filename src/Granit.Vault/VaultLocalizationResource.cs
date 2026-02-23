// ---------------------------------------------------------------------------
// VaultLocalizationResource.cs
// Classe marker de la ressource de localisation du module Vault.
// Hérite de GranitLocalizationResource pour les messages communs.
// ---------------------------------------------------------------------------

using Granit.Localization;
using Granit.Localization.Attributes;

namespace Granit.Vault;

/// <summary>
/// Ressource de localisation du module Vault.
/// </summary>
[LocalizationResourceName("Vault")]
[InheritResource(typeof(GranitLocalizationResource))]
public sealed class VaultLocalizationResource;
