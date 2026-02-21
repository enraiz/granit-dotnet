// =============================================================================
// CurrentTimezoneProvider - Fuseau horaire per-request via AsyncLocal
// =============================================================================
// Enregistre en Singleton. Le stockage est gere par le runtime via AsyncLocal :
// chaque contexte d'execution async recoit sa propre copie de la valeur.
//
// Usage typique :
//   1. Un middleware HTTP lit le fuseau depuis un header/cookie/claim
//   2. Il affecte timezoneProvider.Timezone = "Europe/Brussels"
//   3. Le code en aval (handlers, services) utilise IClock.ConvertToUserTime()
//
// Inspire de Volo.Abp.Timing.CurrentTimezoneProvider.
// =============================================================================

namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Implementation de <see cref="ICurrentTimezoneProvider"/> basee sur <see cref="AsyncLocal{T}"/>.
/// Thread-safe et isolee par contexte d'execution async.
/// </summary>
public sealed class CurrentTimezoneProvider : ICurrentTimezoneProvider
{
    private readonly AsyncLocal<string?> _current = new();

    /// <inheritdoc />
    public string? Timezone
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
