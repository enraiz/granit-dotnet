using Granit.BlobStorage.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.BlobStorage.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IBlobDescriptorStore"/>.
/// </summary>
/// <remarks>
/// All reads are implicitly scoped to the current tenant via the <see cref="IMultiTenant"/>
/// query filter applied by <c>ApplyGranitConventions</c> on <see cref="BlobStorageDbContext"/>.
/// Each operation creates and disposes its own <see cref="BlobStorageDbContext"/> via
/// <see cref="IDbContextFactory{TContext}"/>, making it safe for concurrent request handling.
/// </remarks>
internal sealed class EfBlobDescriptorStore(
    IDbContextFactory<BlobStorageDbContext> contextFactory) : IBlobDescriptorStore
{
    /// <inheritdoc/>
    public async Task<BlobDescriptor?> FindAsync(
        Guid blobId,
        CancellationToken cancellationToken = default)
    {
        await using BlobStorageDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.Blobs
            .FirstOrDefaultAsync(b => b.Id == blobId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(
        BlobDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        await using BlobStorageDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Blobs.Add(descriptor);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(
        BlobDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        await using BlobStorageDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Blobs.Update(descriptor);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
