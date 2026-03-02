using Granit.BlobStorage.Exceptions;
using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Timing;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.Internal;

/// <summary>
/// Default orchestrator for blob storage operations.
/// Coordinates tenant resolution, key strategy, pre-signed URL generation, and descriptor persistence.
/// </summary>
internal sealed class DefaultBlobStorage(
    IBlobDescriptorStore store,
    IBlobKeyStrategy keyStrategy,
    IBlobStorageClient storageClient,
    IGuidGenerator guidGenerator,
    IClock clock,
    ICurrentTenant currentTenant,
    IOptions<BlobStorageOptions> options) : IBlobStorage
{
    private BlobStorageOptions Options => options.Value;

    /// <inheritdoc/>
    public async Task<PresignedUploadTicket> InitiateUploadAsync(
        string containerName,
        BlobUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid blobId = guidGenerator.Create();
        string objectKey = keyStrategy.BuildObjectKey(containerName, blobId);
        string bucket = keyStrategy.ResolveBucketName(containerName);
        string tenantId = currentTenant.IsAvailable && currentTenant.Id is not null
            ? currentTenant.Id.Value.ToString()
            : string.Empty;

        var descriptor = BlobDescriptor.Create(
            id: blobId,
            tenantId: tenantId,
            containerName: containerName,
            objectKey: objectKey,
            request: request,
            createdAt: clock.Now);

        await store.SaveAsync(descriptor, cancellationToken).ConfigureAwait(false);

        PresignedUploadTicket ticket = await storageClient.GenerateUploadTicketAsync(
            bucket,
            objectKey,
            blobId,
            request,
            Options.UploadUrlExpiry,
            cancellationToken).ConfigureAwait(false);

        return ticket;
    }

    /// <inheritdoc/>
    public async Task<PresignedDownloadUrl> CreateDownloadUrlAsync(
        string containerName,
        Guid blobId,
        DownloadUrlOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        BlobDescriptor descriptor = await FindOrThrowAsync(containerName, blobId, cancellationToken).ConfigureAwait(false);

        if (descriptor.Status != BlobStatus.Valid)
        {
            throw new BlobNotValidException(blobId, descriptor.Status);
        }

        TimeSpan expiry = options?.Expiry ?? Options.DownloadUrlExpiry;
        string bucket = keyStrategy.ResolveBucketName(containerName);

        return await storageClient.GenerateDownloadUrlAsync(
            bucket,
            descriptor.ObjectKey,
            options,
            expiry,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<BlobDescriptor?> GetDescriptorAsync(
        string containerName,
        Guid blobId,
        CancellationToken cancellationToken = default) =>
        await store.FindAsync(blobId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string containerName,
        Guid blobId,
        string? deletionReason = null,
        CancellationToken cancellationToken = default)
    {
        BlobDescriptor? descriptor = await store.FindAsync(blobId, cancellationToken).ConfigureAwait(false);

        if (descriptor is null)
        {
            throw new BlobNotFoundException(blobId, containerName);
        }

        // Idempotency: already deleted -> no-op.
        if (descriptor.Status == BlobStatus.Deleted)
        {
            return;
        }

        string bucket = keyStrategy.ResolveBucketName(containerName);

        await storageClient.DeleteObjectAsync(bucket, descriptor.ObjectKey, cancellationToken).ConfigureAwait(false);

        descriptor.MarkAsDeleted(clock.Now, deletionReason);
        await store.UpdateAsync(descriptor, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BlobDescriptor> FindOrThrowAsync(
        string containerName,
        Guid blobId,
        CancellationToken cancellationToken)
    {
        BlobDescriptor? descriptor = await store.FindAsync(blobId, cancellationToken).ConfigureAwait(false);
        if (descriptor is null)
        {
            throw new BlobNotFoundException(blobId, containerName);
        }

        return descriptor;
    }
}
