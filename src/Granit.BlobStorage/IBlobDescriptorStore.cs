namespace Granit.BlobStorage;

/// <summary>
/// Persistence abstraction for <see cref="BlobDescriptor"/> records.
/// </summary>
/// <remarks>
/// Implemented by <c>Granit.BlobStorage.EntityFrameworkCore</c>.
/// All reads are implicitly scoped to the current tenant.
/// </remarks>
public interface IBlobDescriptorStore
{
    /// <summary>
    /// Returns the descriptor for <paramref name="blobId"/> within the current tenant,
    /// or <c>null</c> if not found.
    /// </summary>
    Task<BlobDescriptor?> FindAsync(Guid blobId, CancellationToken cancellationToken = default);

    /// <summary>Persists a newly created <see cref="BlobDescriptor"/>.</summary>
    Task SaveAsync(BlobDescriptor descriptor, CancellationToken cancellationToken = default);

    /// <summary>Persists state changes on an existing <see cref="BlobDescriptor"/>.</summary>
    Task UpdateAsync(BlobDescriptor descriptor, CancellationToken cancellationToken = default);
}
