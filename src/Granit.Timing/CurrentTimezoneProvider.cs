namespace Granit.Timing;

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
