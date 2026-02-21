// =============================================================================
// ICurrentTimezoneProvider - Fuseau horaire de l'utilisateur courant
// =============================================================================
// Porte le fuseau horaire de la requete courante via AsyncLocal.
// Permet a IClock.ConvertToUserTime() de connaitre le fuseau cible.
//
// Le fuseau est un identifiant IANA (ex: "Europe/Brussels", "America/New_York")
// ou Windows (ex: "Romance Standard Time"). TimeZoneInfo.FindSystemTimeZoneById()
// accepte les deux formats sur .NET 6+.
//
// Inspire de Volo.Abp.Timing.ICurrentTimezoneProvider.
// =============================================================================

namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Fournit le fuseau horaire de l'utilisateur courant (per-request via AsyncLocal).
/// </summary>
public interface ICurrentTimezoneProvider
{
    /// <summary>
    /// Identifiant du fuseau horaire courant (IANA ou Windows).
    /// <c>null</c> si aucun fuseau n'est defini (les dates restent en UTC).
    /// </summary>
    string? Timezone { get; set; }
}
