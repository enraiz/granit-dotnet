# Granit.BlobStorage.AzureBlob

Azure Blob Storage implementation for `Granit.BlobStorage`. Provides native SAS
token generation for Direct-to-Cloud uploads and downloads — bytes never transit
through the server.

## How it works

When a client calls `IBlobStorage.InitiateUploadAsync()`, the provider generates
a SAS (Shared Access Signature) URL pointing directly to Azure Blob Storage.
The client PUTs bytes to that URL exactly as it would to S3.

```text
Client ──PUT──▶ https://{account}.blob.core.windows.net/{container}/{key}?{sas}
                                                           │
                                               Azure Blob Storage
```

## Registration

```csharp
// Program.cs
builder.AddGranitBlobStorageAzureBlob();

var app = builder.Build();
```

## Configuration

```json
{
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "DefaultContainer": "granit-blobs",
    "UseManagedIdentity": false,
    "ServiceUri": null
  }
}
```

| Key | Default | Description |
| --- | ------- | ----------- |
| `ConnectionString` | *(required)* | Azure Storage connection string (when not using Managed Identity) |
| `DefaultContainer` | *(required)* | Default Azure Blob container name |
| `UseManagedIdentity` | `false` | Use `DefaultAzureCredential` instead of connection string |
| `ServiceUri` | `null` | Storage account URI (required when `UseManagedIdentity` is `true`) |
| `UploadUrlExpiry` | `00:15:00` | TTL for SAS upload tokens |
| `DownloadUrlExpiry` | `00:05:00` | TTL for SAS download tokens |
| `TenantIsolation` | `Prefix` | Multi-tenant strategy: `Prefix` or `Container` |

## Authentication modes

### Connection string (development / on-premises)

```json
{
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "DefaultContainer": "dev-blobs"
  }
}
```

### Managed Identity (production)

```json
{
  "BlobStorage": {
    "UseManagedIdentity": true,
    "ServiceUri": "https://myaccount.blob.core.windows.net",
    "DefaultContainer": "prod-blobs"
  }
}
```

Requires the `Storage Blob Data Contributor` role on the Azure AD identity.

## Multi-tenant isolation

Blob name format: `{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}`

All tenants share a single Azure container by default (`Prefix` strategy).
Date components distribute blobs across a wider virtual directory hierarchy.

## Dependencies

- `Granit.BlobStorage` (core abstractions)
- `Azure.Storage.Blobs` (Azure SDK)
- `Azure.Identity` (Managed Identity / DefaultAzureCredential)

## License

Apache-2.0
