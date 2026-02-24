namespace Granit.BlobStorage.Exceptions;

/// <summary>
/// Thrown when a download URL is requested for a blob that is not in <see cref="BlobStatus.Valid"/> state.
/// </summary>
public sealed class BlobNotValidException(Guid blobId, BlobStatus currentStatus)
    : Exception($"Blob '{blobId}' cannot be downloaded: current status is '{currentStatus}' (expected '{BlobStatus.Valid}').");
