namespace Granit.BlobStorage.S3;

/// <summary>
/// Multi-tenant data isolation strategy for S3 storage.
/// </summary>
public enum BlobTenantIsolation
{
    /// <summary>
    /// Default strategy. All tenants share a single bucket;
    /// the framework silently prefixes every object key with <c>{tenantId}/</c>.
    /// Key format: <c>{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>.
    /// No bucket limits; recommended for most deployments.
    /// </summary>
    Prefix,

    /// <summary>
    /// One S3 bucket per tenant. Provides true IAM-level isolation.
    /// Bucket name is resolved dynamically from the active tenant.
    /// Subject to cloud provider bucket limits (verify with your provider before using).
    /// </summary>
    Bucket,
}
