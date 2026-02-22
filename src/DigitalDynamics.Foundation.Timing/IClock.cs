namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Abstraction pour l'acces au temps systeme et les conversions de fuseau horaire.
/// Utilise <see cref="TimeProvider"/> en interne pour <see cref="Now"/>.
/// </summary>
public interface IClock
{
    /// <summary>Obtient l'instant present en UTC.</summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Indique si le Clock supporte les fuseaux horaires multiples.
    /// Retourne <c>true</c> quand le Clock fonctionne en UTC (cas standard HDS).
    /// </summary>
    bool SupportsMultipleTimezone { get; }

    /// <summary>
    /// Normalise un <see cref="DateTimeOffset"/> en UTC.
    /// Garantit que meme un offset local (+02:00) est converti en UTC (+00:00)
    /// avant persistance.
    /// </summary>
    DateTimeOffset Normalize(DateTimeOffset dateTime);

    /// <summary>
    /// Convertit un <see cref="DateTimeOffset"/> UTC vers le fuseau horaire
    /// de l'utilisateur courant (via <see cref="ICurrentTimezoneProvider"/>).
    /// Si aucun fuseau n'est configure, retourne la valeur inchangee.
    /// </summary>
    DateTimeOffset ConvertToUserTime(DateTimeOffset utcDateTime);

    /// <summary>
    /// Convertit un <see cref="DateTimeOffset"/> du fuseau utilisateur vers UTC.
    /// Si aucun fuseau n'est configure, retourne la valeur inchangee.
    /// </summary>
    DateTimeOffset ConvertToUtc(DateTimeOffset dateTime);
}
