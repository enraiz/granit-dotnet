using Granit.BlobStorage.Domain;

namespace Granit.BlobStorage;

/// <summary>
/// Read-only persistence abstraction for <see cref="BlobDescriptor"/> records.
/// </summary>
/// <remarks>
/// Implemented by <c>Granit.BlobStorage.EntityFrameworkCore</c>.
/// All reads are implicitly scoped to the current tenant.
/// </remarks>
public interface IBlobDescriptorReader
{
    /// <summary>
    /// Returns the descriptor for <paramref name="blobId"/> within the current tenant,
    /// or <c>null</c> if not found.
    /// </summary>
    Task<BlobDescriptor?> FindAsync(Guid blobId, CancellationToken cancellationToken = default);
}
