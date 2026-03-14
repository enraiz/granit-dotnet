using Granit.Core.MultiTenancy;
using Granit.Timing;

namespace Granit.BlobStorage.Database.Internal;

/// <summary>
/// Builds tenant-prefixed object keys for database blob storage.
/// </summary>
/// <remarks>
/// Key format: <c>{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>.
/// The bucket name is unused — all blobs share the same database table.
/// </remarks>
internal sealed class DatabaseBlobKeyStrategy(
    ICurrentTenant currentTenant,
    IClock clock) : IBlobKeyStrategy
{
    /// <inheritdoc/>
    public string BuildObjectKey(string containerName, Guid blobId)
    {
        DateTimeOffset now = clock.Now;
        string? tenantId = currentTenant.IsAvailable && currentTenant.Id is not null
            ? currentTenant.Id.Value.ToString()
            : null;

        return tenantId is not null
            ? $"{tenantId}/{containerName}/{now:yyyy}/{now:MM}/{blobId}"
            : $"{containerName}/{now:yyyy}/{now:MM}/{blobId}";
    }

    /// <inheritdoc/>
    public string ResolveBucketName(string containerName) => "database";

    /// <inheritdoc/>
    public bool TryExtractTenantId(string objectKey, out string? tenantId)
    {
        if (string.IsNullOrEmpty(objectKey))
        {
            tenantId = null;
            return false;
        }

        int slashIndex = objectKey.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex <= 0)
        {
            tenantId = null;
            return false;
        }

        tenantId = objectKey[..slashIndex];
        return !string.IsNullOrEmpty(tenantId);
    }
}
