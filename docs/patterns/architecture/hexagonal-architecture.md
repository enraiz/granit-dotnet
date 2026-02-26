# Architecture hexagonale (Ports & Adapters)

## Définition

L'architecture hexagonale sépare la logique métier (le « cœur ») des détails
d'infrastructure (bases de données, services cloud, frameworks) via des **ports**
(interfaces) et des **adaptateurs** (implémentations interchangeables). Le cœur
ne connaît que les ports ; les adaptateurs sont branchés au moment de la
composition (DI).

Dans Granit, chaque module fonctionnel (BlobStorage, Features, BackgroundJobs,
Webhooks, Settings) suit ce pattern : un package « cœur » définit les ports,
et des packages séparés (`*.EntityFrameworkCore`, `*.S3`) fournissent les
adaptateurs.

## Schéma

```mermaid
classDiagram
    direction LR

    class IBlobStorage {
        <<port>>
        +InitiateUploadAsync()
        +CreateDownloadUrlAsync()
        +DeleteAsync()
    }

    class IBlobDescriptorStore {
        <<port>>
        +FindAsync()
        +SaveAsync()
        +UpdateAsync()
    }

    class IBlobStorageClient {
        <<port>>
        +DeleteObjectAsync()
        +HeadObjectAsync()
    }

    class IBlobKeyStrategy {
        <<port>>
        +BuildObjectKey()
        +ResolveBucketName()
    }

    class IBlobValidator {
        <<port>>
        +ValidateAsync()
    }

    class DefaultBlobStorage {
        <<core adapter>>
    }

    class EfBlobDescriptorStore {
        <<EF Core adapter>>
    }

    class S3BlobClient {
        <<S3 adapter>>
    }

    class PrefixBlobKeyStrategy {
        <<S3 adapter>>
    }

    class MagicBytesValidator {
        <<built-in adapter>>
    }

    IBlobStorage <|.. DefaultBlobStorage
    DefaultBlobStorage --> IBlobDescriptorStore
    DefaultBlobStorage --> IBlobStorageClient
    DefaultBlobStorage --> IBlobKeyStrategy
    DefaultBlobStorage --> IBlobValidator

    IBlobDescriptorStore <|.. EfBlobDescriptorStore
    IBlobStorageClient <|.. S3BlobClient
    IBlobKeyStrategy <|.. PrefixBlobKeyStrategy
    IBlobValidator <|.. MagicBytesValidator
```

## Implémentation dans Granit

### BlobStorage (exemple principal)

| Port (interface) | Fichier | Adaptateur(s) |
|------------------|---------|---------------|
| `IBlobStorage` | `src/Granit.BlobStorage/IBlobStorage.cs` | `DefaultBlobStorage` (orchestrateur) |
| `IBlobDescriptorStore` | `src/Granit.BlobStorage/IBlobDescriptorStore.cs` | `EfBlobDescriptorStore` dans `Granit.BlobStorage.EntityFrameworkCore` |
| `IBlobStorageClient` | `src/Granit.BlobStorage/Internal/IBlobStorageClient.cs` | `S3BlobClient` dans `Granit.BlobStorage.S3` |
| `IBlobKeyStrategy` | `src/Granit.BlobStorage/IBlobKeyStrategy.cs` | `PrefixBlobKeyStrategy` dans `Granit.BlobStorage.S3` |
| `IBlobValidator` | `src/Granit.BlobStorage/IBlobValidator.cs` | `MagicBytesValidator`, `MaxSizeValidator` (built-in) + custom |

### Même pattern dans les autres modules

| Module | Port | Adaptateurs |
|--------|------|-------------|
| Features | `IFeatureStore` | `InMemoryFeatureStore`, `EfCoreFeatureStore` |
| BackgroundJobs | `IBackgroundJobStore` | `InMemoryBackgroundJobStore`, `EfBackgroundJobStore` |
| Webhooks | `IWebhookSubscriptionStore` | `EfWebhookSubscriptionStore` |
| Settings | `ISettingStore` | `EfCoreSettingStore` |
| Caching | `ICacheService<T>` | `DistributedCacheService`, `HybridCacheService` |
| Encryption | `IStringEncryptionProvider` | `AesStringEncryptionProvider` |

## Justification

| Problème | Solution |
|----------|----------|
| Couplage à un fournisseur cloud (S3, Azure Blob) | Les ports permettent de changer d'adaptateur sans toucher au cœur |
| Tests unitaires nécessitant une base de données | `InMemoryFeatureStore` et `InMemoryBackgroundJobStore` remplacent EF Core en test |
| Conformité HDS — pouvoir migrer d'OVHcloud S3 vers un autre provider souverain | Implémenter `IBlobStorageClient` pour le nouveau provider suffit |
| Packages NuGet indépendants | Le cœur (`Granit.BlobStorage`) n'a aucune dépendance sur EF Core ou AWS SDK |

## Exemple d'usage

```csharp
// Remplacer S3 par MinIO — seul l'adaptateur change
services.AddSingleton<IBlobStorageClient, MinioBlobClient>();
services.AddSingleton<IBlobKeyStrategy, MinioBlobKeyStrategy>();

// Le reste du code applicatif ne change pas
IBlobStorage blobStorage = serviceProvider.GetRequiredService<IBlobStorage>();
PresignedUploadTicket ticket = await blobStorage.InitiateUploadAsync(
    "medical-documents",
    new BlobUploadRequest("rapport-irm.pdf", "application/pdf", MaxAllowedBytes: 50_000_000),
    cancellationToken);
```
