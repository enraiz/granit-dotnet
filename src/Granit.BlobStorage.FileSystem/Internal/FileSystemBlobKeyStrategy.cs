using Granit.BlobStorage.FileSystem.Options;
using Granit.Core.MultiTenancy;
using Granit.Timing;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.FileSystem.Internal;

/// <summary>
/// <see cref="IBlobKeyStrategy"/> using tenant-prefix isolation on the local file system.
/// </summary>
/// <remarks>
/// Object key format (multi-tenant): <c>{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>
/// Object key format (single-tenant): <c>{containerName}/{yyyy}/{MM}/{blobId}</c>
/// <para>
/// The <see cref="ResolveBucketName"/> always returns <see cref="FileSystemBlobOptions.BasePath"/>
/// since the file system provider uses a single root directory.
/// </para>
/// </remarks>
internal sealed class FileSystemBlobKeyStrategy(
    ICurrentTenant currentTenant,
    IClock clock,
    IOptions<FileSystemBlobOptions> options) : IBlobKeyStrategy
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
    public string ResolveBucketName(string containerName) => options.Value.BasePath;

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
