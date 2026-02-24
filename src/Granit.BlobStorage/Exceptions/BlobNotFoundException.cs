namespace Granit.BlobStorage.Exceptions;

/// <summary>
/// Thrown when a <see cref="BlobDescriptor"/> is not found for the current tenant.
/// </summary>
public sealed class BlobNotFoundException(Guid blobId, string containerName)
    : Exception($"Blob '{blobId}' not found in container '{containerName}' for the current tenant.");
