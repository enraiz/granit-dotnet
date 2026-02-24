using Microsoft.EntityFrameworkCore;

namespace Granit.Persistence.Migrations.Internal;

/// <summary>
/// No-op <see cref="ITenantDbIsolator"/> for Shared database and Tenant-per-Database topologies.
/// </summary>
internal sealed class NullTenantDbIsolator : ITenantDbIsolator
{
    /// <inheritdoc/>
    public Task IsolateAsync(DbContext context, Guid tenantId, CancellationToken ct) =>
        Task.CompletedTask;
}
