// =============================================================================
// CurrentTenant - Implémentation de ICurrentTenant via AsyncLocal
// =============================================================================
// Stocke le tenant courant dans AsyncLocal<T> (propagation automatique
// dans les Task et async/await). Chaque flux async dispose de son propre contexte.
//
// Change() empile les surcharges et les restaure via IDisposable.
// Enregistrer comme Singleton : l'état est dans AsyncLocal (static), pas l'instance.
// =============================================================================

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Implémentation de <see cref="ICurrentTenant"/> basée sur <see cref="AsyncLocal{T}"/>.
/// Thread-safe : chaque flux async/await possède son propre contexte tenant.
/// </summary>
public sealed class CurrentTenant : ICurrentTenant
{
    private static readonly AsyncLocal<TenantInfo?> _current = new();

    /// <inheritdoc/>
    public bool IsAvailable => Id.HasValue;

    /// <inheritdoc/>
    public Guid? Id => _current.Value?.Id;

    /// <inheritdoc/>
    public string? Name => _current.Value?.Name;

    /// <inheritdoc/>
    public IDisposable Change(Guid? id, string? name = null)
    {
        TenantInfo? previous = _current.Value;
        _current.Value = id.HasValue ? new TenantInfo(id, name) : null;
        return new TenantScope(previous);
    }

    private sealed class TenantScope : IDisposable
    {
        private readonly TenantInfo? _previous;
        private bool _disposed;

        public TenantScope(TenantInfo? previous) => _previous = previous;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _current.Value = _previous;
            }
        }
    }
}
