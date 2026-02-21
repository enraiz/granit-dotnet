// =============================================================================
// DisableDateTimeNormalizationAttribute - Opt-out de la normalisation automatique
// =============================================================================
// Marqueur pour desactiver la normalisation automatique des DateTimeOffset
// sur une classe, propriete ou parametre.
//
// Usage futur : un EF Core value converter ou un model binder ASP.NET Core
// pourrait normaliser automatiquement les DateTimeOffset en UTC. Ce marqueur
// permet d'exclure certaines proprietes de cette normalisation.
//
// Inspire de Volo.Abp.Timing.DisableDateTimeNormalizationAttribute.
// =============================================================================

namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Desactive la normalisation automatique des DateTimeOffset sur l'element annote.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class DisableDateTimeNormalizationAttribute : Attribute;
