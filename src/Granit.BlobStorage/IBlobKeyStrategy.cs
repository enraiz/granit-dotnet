namespace Granit.BlobStorage;

/// <summary>
/// Builds and parses S3 object keys, enforcing multi-tenant isolation.
/// </summary>
/// <remarks>
/// The default implementation (<c>PrefixBlobKeyStrategy</c> in <c>Granit.BlobStorage.S3</c>)
/// uses the <c>Prefix</c> isolation strategy:
/// <c>{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>.
/// </remarks>
public interface IBlobKeyStrategy
{
    /// <summary>
    /// Builds the full S3 object key for a new blob.
    /// </summary>
    /// <param name="containerName">Logical container (e.g. <c>medical-images</c>).</param>
    /// <param name="blobId">Stable blob identifier.</param>
    string BuildObjectKey(string containerName, Guid blobId);

    /// <summary>
    /// Resolves the physical S3 bucket name for a given logical container.
    /// </summary>
    /// <param name="containerName">Logical container.</param>
    string ResolveBucketName(string containerName);

    /// <summary>
    /// Attempts to extract the tenant identifier from a raw S3 object key.
    /// Used when processing S3 event notifications, where only the raw key is available.
    /// </summary>
    /// <param name="objectKey">Raw S3 object key.</param>
    /// <param name="tenantId">Extracted tenant identifier, or <c>null</c> if not parseable.</param>
    bool TryExtractTenantId(string objectKey, out string? tenantId);
}
