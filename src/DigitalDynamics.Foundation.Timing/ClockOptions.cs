// =============================================================================
// ClockOptions - Configuration du module Timing
// =============================================================================
// Configurable via IOptions<ClockOptions> dans Program.cs :
//   builder.Services.AddFoundationTiming(options =>
//   {
//       options.DefaultTimezone = "Europe/Brussels";
//   });
// =============================================================================

namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Options de configuration pour le module Timing.
/// </summary>
public sealed class ClockOptions
{
    /// <summary>
    /// Fuseau horaire par defaut quand aucun n'est specifie par l'utilisateur
    /// via <see cref="Abstractions.Timing.ICurrentTimezoneProvider"/>.
    /// <c>null</c> = pas de conversion (les dates restent en UTC).
    /// Exemple : <c>"Europe/Brussels"</c>, <c>"America/New_York"</c>
    /// </summary>
    public string? DefaultTimezone { get; set; }
}
