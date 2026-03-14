using Granit.BlobStorage.AzureBlob.Options;
using Granit.Core.MultiTenancy;
using Granit.Timing;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.AzureBlob.Internal;

/// <summary>
/// Default <see cref="IBlobKeyStrategy"/> using tenant-prefix isolation for Azure Blob Storage.
/// </summary>
/// <remarks>
/// Blob name format (multi-tenant): <c>{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>
/// Blob name format (single-tenant): <c>{containerName}/{yyyy}/{MM}/{blobId}</c>
/// <para>
/// The date components improve list-operation performance by distributing
/// blobs across a wider virtual directory hierarchy.
/// </para>
/// </remarks>
internal sealed class AzureBlobKeyStrategy(
    ICurrentTenant currentTenant,
    IClock clock,
    IOptions<AzureBlobOptions> options) : IBlobKeyStrategy
{
    /// <inheritdoc/>
    public string BuildObjectKey(string containerName, Guid blobId)
    {
        string? tenantId = currentTenant.IsAvailable && currentTenant.Id is not null
            ? currentTenant.Id.Value.ToString()
            : null;
        DateTimeOffset now = clock.Now;
        return tenantId is not null
            ? $"{tenantId}/{containerName}/{now:yyyy}/{now:MM}/{blobId}"
            : $"{containerName}/{now:yyyy}/{now:MM}/{blobId}";
    }

    /// <inheritdoc/>
    public string ResolveBucketName(string containerName) => options.Value.DefaultContainer;

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
