using Granit.Core.MultiTenancy;
using Granit.Timing;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.S3.Internal;

/// <summary>
/// Default <see cref="IBlobKeyStrategy"/> using tenant-prefix isolation.
/// </summary>
/// <remarks>
/// Object key format: <c>{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>
/// <para>
/// The date components improve S3 performance on large buckets by distributing
/// keys across a wider key-space prefix, reducing hot-spot partitions.
/// </para>
/// </remarks>
internal sealed class PrefixBlobKeyStrategy(
    ICurrentTenant currentTenant,
    IClock clock,
    IOptions<S3BlobOptions> options) : IBlobKeyStrategy
{
    /// <inheritdoc/>
    public string BuildObjectKey(string containerName, Guid blobId)
    {
        string tenantId = GetRequiredTenantId();
        DateTimeOffset now = clock.Now;
        return $"{tenantId}/{containerName}/{now:yyyy}/{now:MM}/{blobId}";
    }

    /// <inheritdoc/>
    public string ResolveBucketName(string containerName) => options.Value.DefaultBucket;

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

    private string GetRequiredTenantId()
    {
        if (!currentTenant.IsAvailable || currentTenant.Id is null)
        {
            throw new InvalidOperationException(
                "Cannot build a blob object key without an active tenant context.");
        }

        return currentTenant.Id.Value.ToString();
    }
}
