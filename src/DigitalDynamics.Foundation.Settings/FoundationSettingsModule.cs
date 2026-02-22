// =============================================================================
// FoundationSettingsModule - Module Foundation pour les paramètres dynamiques
// =============================================================================
// Configure le système de paramètres avec résolution en cascade :
//   User (U) → Tenant (T) → Global (G) → Configuration (C) → Default (D)
//
// Dépendances :
//   - FoundationCachingModule   : ICacheService<SettingValue> pour le cache des providers
//   - FoundationMultiTenancyModule : ICurrentTenant pour le provider Tenant
//   - FoundationEncryptionModule   : IStringEncryptionService pour IsEncrypted (couche store)
//
// Store par défaut : InMemorySettingStore (remplacé par EfCoreSettingStore en production
//   via FoundationSettingsEntityFrameworkCoreModule).
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Encryption;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Settings.Extensions;
using DigitalDynamics.Foundation.Settings.Options;

namespace DigitalDynamics.Foundation.Settings;

/// <summary>
/// Module Foundation pour la gestion des paramètres dynamiques avec résolution en cascade.
/// </summary>
[DependsOn(typeof(FoundationCachingModule))]
[DependsOn(typeof(FoundationMultiTenancyModule))]
[DependsOn(typeof(FoundationEncryptionModule))]
public sealed class FoundationSettingsModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationSettings(
            context.Configuration.GetSection(SettingsOptions.SectionName));
}
