namespace Granit.BlobStorage.AzureBlob;

/// <summary>
/// Multi-tenant data isolation strategy for Azure Blob Storage.
/// </summary>
public enum BlobTenantIsolation
{
    /// <summary>
    /// Default strategy. All tenants share a single container;
    /// the framework silently prefixes every blob name with <c>{tenantId}/</c>.
    /// Key format: <c>{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>.
    /// No container limits; recommended for most deployments.
    /// </summary>
    Prefix,

    /// <summary>
    /// One Azure container per tenant. Provides true RBAC-level isolation.
    /// Container name is resolved dynamically from the active tenant.
    /// Subject to Azure container naming constraints (verify before using).
    /// </summary>
    Container,
}
