# Stockage de fichiers — Granit.BlobStorage

Stockage d'objets souverain, Direct-to-Cloud, conforme HDS et RGPD.
Le serveur applicatif ne transite jamais les octets — seuls les métadonnées et les URL pré-signées
sont échangés entre le client, le serveur et OVHcloud Object Storage.

| Package | Rôle |
| --- | --- |
| `Granit.BlobStorage` | Core provider-agnostique : `IBlobStorage`, `BlobDescriptor`, pipeline de validation, `IBlobDescriptorStore` |
| `Granit.BlobStorage.S3` | Adaptateur S3 : client AWS SDK, URL pré-signées, `S3BlobOptions` |
| `Granit.BlobStorage.EntityFrameworkCore` | Persistance EF Core : `BlobStorageDbContext`, table `storage_blob_descriptors` |

## Concepts clés

### Architecture Direct-to-Cloud

```text
Client                Serveur (Granit)           OVHcloud S3
  │                        │                          │
  │─ POST /upload ─────────▶│                          │
  │                        │─ InitiateUploadAsync()   │
  │                        │  crée BlobDescriptor     │
  │                        │  génère URL pré-signée ──▶│
  │◀─ PresignedUploadTicket ─│                          │
  │                          │                          │
  │─ PUT (octets) ────────────────────────────────────▶│
  │◀─ 200 OK ─────────────────────────────────────────│
  │                          │                          │
  │─ POST /validate ─────────▶│                          │
  │                          │─ ValidateAsync()         │
  │                          │  magic bytes (range GET)─▶│
  │                          │◀─ premiers 261 octets ───│
  │                          │  taille (HEAD) ──────────▶│
  │                          │◀─ Content-Length ────────│
  │                          │  BlobDescriptor → Valid  │
  │◀─ 200 OK ────────────────│                          │
```

Le serveur ne lit jamais le fichier complet : 261 octets max pour la détection de type,
`Content-Length` S3 pour la taille.

### Cycle de vie du BlobDescriptor

```text
Pending → Uploading → Valid → Deleted
                    ↘ Rejected
```

| État | Déclencheur |
| --- | --- |
| `Pending` | `InitiateUploadAsync()` — ticket émis, upload pas encore reçu |
| `Uploading` | Notification S3 reçue — validation en cours |
| `Valid` | Tous les validateurs ont passé |
| `Rejected` | Un validateur a échoué — objet S3 déjà supprimé |
| `Deleted` | `DeleteAsync()` — Crypto-Shredding RGPD |

> **HDS / RGPD** : le `BlobDescriptor` n'est **jamais supprimé de la base**.
> `Deleted` signifie que les octets S3 sont effacés ; la piste d'audit reste 3 ans.

### Isolation multi-tenant

La clé S3 suit le format `{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}`.
Le préfixe `tenantId/` garantit le cloisonnement sans bucket dédié par tenant.
`IBlobDescriptorStore.FindAsync` filtre systématiquement par `TenantId` du tenant actif.

## Installation

### 1 — Module

```csharp
[DependsOn(
    typeof(GranitBlobStorageModule),
    typeof(GranitBlobStorageEntityFrameworkCoreModule))]
public sealed class MyAppModule : GranitModule { }
```

### 2 — Configuration

```json
// appsettings.json
{
  "BlobStorage": {
    "ServiceUrl": "https://s3.rbx.io.cloud.ovh.net",
    "Region": "rbx",
    "DefaultBucket": "my-blobs",
    "ForcePathStyle": false,
    "AccessKey": "INJECTER_DEPUIS_VAULT",
    "SecretKey": "INJECTER_DEPUIS_VAULT"
  }
}
```

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `ServiceUrl` | `string` | — | Endpoint S3 (obligatoire) |
| `AccessKey` | `string` | — | Clé d'accès — injecter depuis Granit.Vault |
| `SecretKey` | `string` | — | Clé secrète — injecter depuis Granit.Vault |
| `Region` | `string` | `us-east-1` | Région S3. OVHcloud Roubaix : `rbx` |
| `DefaultBucket` | `string` | — | Bucket S3 par défaut (obligatoire) |
| `ForcePathStyle` | `bool` | `true` | Activer pour MinIO et certains providers |
| `TenantIsolation` | `BlobTenantIsolation` | `Prefix` | Stratégie d'isolation : `Prefix` (1 bucket, préfixe tenant) |

> **Production OVHcloud** : `ServiceUrl = https://s3.rbx.io.cloud.ovh.net`, `Region = rbx`, `ForcePathStyle = false`.
>
> **Développement MinIO** : `ServiceUrl = http://localhost:9000`, `ForcePathStyle = true`.

### 3 — Enregistrement des services

```csharp
// Fournisseur S3 (obligatoire)
builder.AddGranitBlobStorageS3();

// Persistance EF Core (obligatoire en production)
builder.AddGranitBlobStorageEntityFrameworkCore(options =>
    options.UseNpgsql(connectionString));
```

### 4 — Migrations EF Core

```bash
dotnet ef migrations add InitBlobStorage \
  --project src/Granit.BlobStorage.EntityFrameworkCore \
  --startup-project src/MyApp
```

La migration crée la table `storage_blob_descriptors` avec un index unique sur `ObjectKey`
et un index composite sur `(TenantId, ContainerName)`.

## Utilisation

### Initier un upload

```csharp
PresignedUploadTicket ticket = await blobStorage.InitiateUploadAsync(
    containerName: "prescriptions",
    request: new BlobUploadRequest(
        FileName: "ordonnance.pdf",
        ContentType: "application/pdf",
        MaxAllowedBytes: 10_000_000L),   // limite par upload, persistée dans BlobDescriptor
    cancellationToken: ct);

// Retourner au client : ticket.BlobId, ticket.UploadUrl, ticket.ExpiresAt
```

### Déclencher la validation post-upload

Appeler après avoir reçu la confirmation de l'upload S3 (webhook, polling, etc.) :

```csharp
await blobStorage.ValidateAsync(
    containerName: "prescriptions",
    blobId: ticket.BlobId,
    cancellationToken: ct);
```

La validation exécute le pipeline ordonné : `MagicBytesValidator` (Order=10) puis
`MaxSizeValidator` (Order=20). Le `BlobDescriptor` passe à `Valid` ou `Rejected`.

### Générer une URL de téléchargement

```csharp
PresignedDownloadUrl url = await blobStorage.CreateDownloadUrlAsync(
    containerName: "prescriptions",
    blobId: descriptorId,
    options: new DownloadUrlOptions { ExpiryOverride = TimeSpan.FromMinutes(30) },
    cancellationToken: ct);

// url.Url : URL pré-signée à transmettre au client
// url.ExpiresAt : horodatage d'expiration
```

Lève `BlobNotFoundException` si le blob n'existe pas pour le tenant actif.
Lève `BlobNotValidException` si le blob n'est pas à l'état `Valid`.

## Exceptions

| Classe | Code HTTP | `ErrorCode` | Déclencheur |
| --- | --- | --- | --- |
| `BlobNotFoundException` | 404 | `BlobStorage:NotFound` | Blob introuvable pour le tenant actif |
| `BlobNotValidException` | 400 | `BlobStorage:NotValid` | Blob non à l'état `Valid` lors du téléchargement |

Les deux exceptions implémentent `IHasErrorCode`. Si `Granit.Localization` est configuré,
le titre renvoyé au client est résolu depuis les fichiers JSON du module
(`Localization/BlobStorage/{culture}.json`) :

```json
// fr
{
  "BlobStorage:NotFound": "Le fichier demandé est introuvable.",
  "BlobStorage:NotValid": "Le fichier n'est pas disponible au téléchargement."
}
```

`BlobNotFoundException` hérite de `NotFoundException` (→ 404).
`BlobNotValidException` hérite de `Exception` (→ 400 via `IHasErrorCode`).

### Supprimer un fichier (Crypto-Shredding RGPD)

```csharp
await blobStorage.DeleteAsync(
    containerName: "prescriptions",
    blobId: descriptorId,
    deletionReason: "RGPD Art. 17 — demande d'effacement",
    cancellationToken: ct);
```

L'objet S3 est physiquement supprimé. Le `BlobDescriptor` reste en base
avec `Status = Deleted`, `DeletedAt` et `DeletionReason`.

## Pipeline de validation

Les validateurs implémentent `IBlobValidator` et sont exécutés dans l'ordre croissant de `Order`.

| Validateur | Order | Description |
| --- | --- | --- |
| `MagicBytesValidator` | 10 | Détecte le vrai type MIME via les magic bytes (range GET S3, 261 octets max) |
| `MaxSizeValidator` | 20 | Vérifie que `ActualSizeBytes ≤ MaxAllowedBytes` (S3 HEAD, sans téléchargement) |

### Types MIME détectés par MagicBytesValidator

| Signature | MIME | Offset |
| --- | --- | --- |
| `%PDF` | `application/pdf` | 0 |
| `FF D8 FF` | `image/jpeg` | 0 |
| `89 50 4E 47...` | `image/png` | 0 |
| `GIF87a` / `GIF89a` | `image/gif` | 0 |
| `49 49 2A 00` / `4D 4D 00 2A` | `image/tiff` | 0 |
| `PK 03 04` | `application/zip` | 0 |
| `DICM` | `application/dicom` | 128 |

Types inconnus : **pass-through conservateur** — le blob passe avec le type déclaré.
Évite les faux négatifs sur des formats propriétaires ou peu courants.

### Ajouter un validateur personnalisé

```csharp
// Enregistrer avant AddGranitBlobStorageS3()
builder.Services.AddScoped<IBlobValidator, AntivirusValidator>();
```

Le validateur sera automatiquement inclus dans le pipeline, trié par `Order`.

## BlobDescriptor

```csharp
public sealed class BlobDescriptor
{
    public Guid Id { get; }
    public string TenantId { get; }
    public string ContainerName { get; }
    public string ObjectKey { get; }           // clé S3 complète
    public string OriginalFileName { get; }
    public string DeclaredContentType { get; } // déclaré à l'upload
    public long MaxAllowedBytes { get; }
    public string? VerifiedContentType { get; } // confirmé par magic bytes
    public long? SizeBytes { get; }            // taille réelle S3
    public BlobStatus Status { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ValidatedAt { get; }
    public DateTimeOffset? DeletedAt { get; }
    public string? RejectionReason { get; }
    public string? DeletionReason { get; }
}
```

## Architecture interne

```text
IBlobStorage (DefaultBlobStorage)
  ├── IBlobKeyStrategy (PrefixBlobKeyStrategy)
  │     └── {tenantId}/{containerName}/{yyyy}/{MM}/{blobId}
  ├── IBlobDescriptorStore (EfBlobDescriptorStore)
  │     └── BlobStorageDbContext → table storage_blob_descriptors
  ├── IBlobStorageClient (S3BlobClient)
  │     └── AmazonS3Client — URL pré-signées, Range GET, HEAD, DELETE (thread-safe, Singleton)
  └── IEnumerable<IBlobValidator> (pipeline ordonné)
        ├── MagicBytesValidator (Order=10) — MagicByteDetector.Detect()
        └── MaxSizeValidator (Order=20)
```

## Roadmap

| Story | Statut | Description |
| --- | --- | --- |
| #172 | ✅ Terminé | `IBlobStorage.InitiateUploadAsync` — ticket pré-signé S3 |
| #173 | ✅ Terminé | `IBlobStorage.CreateDownloadUrlAsync` — URL de téléchargement sécurisée |
| #174 | ✅ Terminé | `IBlobKeyStrategy` — isolation multi-tenant par préfixe `{tenantId}/` |
| #175 | ✅ Terminé | Pipeline `IBlobValidator` — magic bytes, taille, extensible |
| #176 | ✅ Terminé | `IBlobDescriptorStore` — persistance EF Core, isolation tenant, piste HDS |
| #177 | ✅ Terminé | `IBlobStorage.DeleteAsync` — Crypto-Shredding RGPD, conservation audit |
| #178 | ✅ Terminé | `S3BlobOptions` — configuration OVHcloud / MinIO, validation au démarrage |

## Conformité HDS / RGPD

- **Souveraineté** : `ServiceUrl` doit pointer sur OVHcloud FR (`s3.rbx.io.cloud.ovh.net`).
  Ne jamais utiliser AWS S3, Azure Blob ou GCP Cloud Storage pour des données de santé.
- **Direct-to-Cloud** : les octets ne transitent jamais par le serveur applicatif.
  Réduit la surface d'attaque et les coûts de bande passante.
- **Crypto-Shredding** : `DeleteAsync` efface l'objet S3 (données irrécupérables),
  puis conserve le `BlobDescriptor` 3 ans pour la piste d'audit HDS.
- **Isolation tenant** : `EfBlobDescriptorStore.FindAsync` filtre par `TenantId` du tenant actif —
  un tenant ne peut pas accéder aux blobs d'un autre, même avec un `BlobId` valide.
- **Credentials** : `AccessKey` et `SecretKey` ne doivent jamais apparaître en clair.
  Injecter depuis `Granit.Vault` (credentials dynamiques) en production.
- **Validation** : `MagicBytesValidator` détecte les fichiers malveillants déguisés
  (ex. exécutable renommé en `.pdf`). Tout blob non reconnu passe par défaut
  (pas de faux négatif sur un format légitime inconnu).

## Dépendances Granit

| Package | Dépend de | Utilisé par |
|---------|-----------|-------------|
| `Granit.BlobStorage` | `Granit.Core`, `Granit.Guids`, `Granit.Timing` | `BlobStorage.EntityFrameworkCore`, `BlobStorage.S3` |
| `Granit.BlobStorage.S3` | `Granit.BlobStorage`, `Granit.Timing` | Module feuille |
| `Granit.BlobStorage.EntityFrameworkCore` | `Granit.BlobStorage` | Module feuille |

> **Dépendance justifiée** : `BlobStorage.S3 → Timing` — `PrefixBlobKeyStrategy`
> injecte `IClock` directement pour intégrer des segments `yyyy/MM` dans les clés
> S3. Ce sharding par date distribue les objets sur plusieurs partitions de préfixe,
> évitant les hot-spots sur les buckets volumineux. La référence explicite dans le
> csproj est correcte (ne pas se fier aux transitives pour les types consommés
> directement).
>
> Les messages d'erreur (`BlobStorage:NotFound`, `BlobStorage:NotValid`) sont
> localisés par `GranitExceptionHandler` via les JSON embarqués, découverts
> automatiquement par `LocalizationAutoDiscovery`.
>
> Voir le [graphe de dépendances complet](../dependencies.md).
