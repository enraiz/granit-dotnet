namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Implementation of <see cref="ICurrentTenant"/> based on <see cref="AsyncLocal{T}"/>.
/// Thread-safe: each async/await flow has its own tenant context.
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

    private sealed class TenantScope(TenantInfo? previous) : IDisposable
    {
        private readonly TenantInfo? _previous = previous;
        private bool _disposed;

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
