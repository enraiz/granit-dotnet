using Granit.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Granit.BlobStorage.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IBlobDescriptorStore"/>.
/// </summary>
/// <remarks>
/// All reads are implicitly scoped to the current tenant via <see cref="ICurrentTenant"/>.
/// Each operation creates and disposes its own <see cref="BlobStorageDbContext"/> via
/// <see cref="IDbContextFactory{TContext}"/>, making it safe for concurrent request handling.
/// </remarks>
internal sealed class EfBlobDescriptorStore(
    IDbContextFactory<BlobStorageDbContext> contextFactory,
    ICurrentTenant currentTenant) : IBlobDescriptorStore
{
    /// <inheritdoc/>
    public async Task<BlobDescriptor?> FindAsync(
        Guid blobId,
        CancellationToken cancellationToken = default)
    {
        string tenantId = GetRequiredTenantId();
        await using BlobStorageDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Blobs
            .FirstOrDefaultAsync(b => b.Id == blobId && b.TenantId == tenantId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(
        BlobDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        await using BlobStorageDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Blobs.Add(descriptor);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(
        BlobDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        await using BlobStorageDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Blobs.Update(descriptor);
        await context.SaveChangesAsync(cancellationToken);
    }

    private string GetRequiredTenantId()
    {
        if (!currentTenant.IsAvailable || currentTenant.Id is null)
        {
            throw new InvalidOperationException(
                "Cannot query blob descriptors without an active tenant context.");
        }

        return currentTenant.Id.Value.ToString();
    }
}
