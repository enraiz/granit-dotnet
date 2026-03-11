# Core

`Granit.Core` est le package fondation de tous les autres packages
Granit. Il fournit :

- Le **système de modules** (voir [modularity.md](modularity.md)) : `GranitModule`,
  `[DependsOn]`, `AddGranitAsync<T>()`, tri topologique
- Les **types domaine partagés** (voir [domain.md](../data/domain.md)) : hiérarchie d'entités
  (`Entity`, `CreationAuditedEntity`, `AuditedEntity`, `FullAuditedEntity`),
  `ISoftDeletable`, `IMultiTenant`, `IActive`, `AuditLogEntry`
- Le **service de data filtering** (voir [data-filtering.md](../data/data-filtering.md)) :
  `IDataFilter` — bypass sélectif des query filters globaux EF Core
- Le **contexte de tenant minimal** : `ICurrentTenant` (namespace `Granit.Core.MultiTenancy`) —
  interface disponible dans tous les modules sans référencer `Granit.MultiTenancy`
  (voir [Dépendance optionnelle sur le multi-tenancy](#dépendance-optionnelle-sur-le-multi-tenancy))

Ce package remplace l'ancien `Granit.Abstractions`. Les interfaces de service
(`IClock`, `IGuidGenerator`, `ICurrentUserService`, `ITransitEncryptionService`) vivent
désormais dans leurs modules respectifs (voir section
[Migration depuis Abstractions](#migration-depuis-abstractions)).

## Installation

```bash
dotnet add package Granit.Core
```

Ce package est automatiquement tiré comme dépendance transitive par tous les packages
Granit. Il n'est nécessaire de le référencer explicitement que dans les projets qui
utilisent les types domaine sans autre package Granit.

## Système de modules

Le système de modules est documenté dans [modularity.md](modularity.md).

## Types domaine

La hiérarchie d'entités et les interfaces domaine (`ISoftDeletable`, `IMultiTenant`,
`IActive`, `AuditLogEntry`) sont documentées dans [domain.md](../data/domain.md).

## Data Filtering

`IDataFilter` et son implémentation `AsyncLocal` sont documentés dans
[data-filtering.md](../data/data-filtering.md).

## Architecture

```text
Granit.Core
├── Domain/
│   ├── Entity.cs                   (classe de base, identifiant)
│   ├── CreationAuditedEntity.cs    (+ CreatedAt, CreatedBy)
│   ├── AuditedEntity.cs            (+ ModifiedAt, ModifiedBy)
│   ├── FullAuditedEntity.cs        (+ ISoftDeletable)
│   ├── ISoftDeletable.cs           (suppression logique RGPD)
│   ├── IMultiTenant.cs             (isolation par tenant, TenantId auto-injecté)
│   ├── IActive.cs                  (filtre actif/inactif, WHERE IsActive = true)
│   └── AuditLogEntry.cs            (entrée d'audit trail)
├── DataFiltering/
│   ├── IDataFilter.cs              (interface de contrôle runtime des query filters)
│   └── DataFilter.cs               (implémentation AsyncLocal, Singleton)
├── MultiTenancy/
│   ├── ICurrentTenant.cs           (interface — disponible sans Granit.MultiTenancy)
│   └── NullTenantContext.cs        (Null Object, enregistré par défaut via TryAddSingleton)
├── Modularity/
│   ├── GranitModule.cs         (classe de base des modules)
│   ├── DependsOnAttribute.cs       (déclaration de dépendances)
│   ├── ServiceConfigurationContext.cs
│   ├── ApplicationInitializationContext.cs
│   ├── ModuleDescriptor.cs         (internal)
│   ├── ModuleLoader.cs             (internal, tri topologique)
│   └── GranitApplication.cs    (singleton, orchestrateur lifecycle)
└── Extensions/
    ├── GranitHostBuilderExtensions.cs   (AddGranit<T> / AddGranitAsync<T>)
    └── GranitApplicationExtensions.cs   (UseGranit / UseGranitAsync)
```

## Dépendance optionnelle sur le multi-tenancy

`ICurrentTenant` a été promu dans `Granit.Core.MultiTenancy` afin de rompre le couplage
entre les modules métier (`Granit.Persistence`, `Granit.Settings`, `Granit.Wolverine`, etc.)
et `Granit.MultiTenancy`.

### Principe

`AddGranit<T>()` enregistre un **Null Object** comme valeur par défaut avant d'exécuter les
modules :

```csharp
// GranitHostBuilderExtensions.cs — exécuté avant ConfigureServices de chaque module
builder.Services.TryAddSingleton<ICurrentTenant>(NullTenantContext.Instance);
```

`NullTenantContext` retourne `IsAvailable = false`, `Id = null` et une implémentation
no-op de `Change()`. Si `Granit.MultiTenancy` est présent dans le graphe de modules,
il remplace l'enregistrement par défaut avec l'implémentation réelle :

```csharp
// MultiTenancyServiceCollectionExtensions.cs
services.Replace(ServiceDescriptor.Singleton<ICurrentTenant, CurrentTenant>());
```

### Conséquences

| Scénario | `ICurrentTenant.IsAvailable` | Comportement |
| --- | --- | --- |
| `Granit.MultiTenancy` absent | `false` | `NullTenantContext` — pas d'isolation tenant |
| `Granit.MultiTenancy` présent, pas de requête active | `false` | Hors requête HTTP / job sans tenant |
| `Granit.MultiTenancy` présent, requête avec tenant | `true` | Tenant résolu via JWT ou en-tête |

Les modules qui consomment `ICurrentTenant` doivent **toujours vérifier `IsAvailable`**
avant d'utiliser `Id` — ce comportement était déjà attendu avant ce changement.

### Modules avec dépendance forte maintenue

Les modules `Granit.BlobStorage`, `Granit.BlobStorage.S3` et
`Granit.BlobStorage.EntityFrameworkCore` conservent une dépendance **explicite** sur
`Granit.MultiTenancy` : le stockage de blobs impose une isolation tenant stricte pour
des raisons RGPD/ISO 27001. Tenter de les utiliser sans contexte tenant lève une exception.

## Migration depuis Abstractions

`Granit.Abstractions` a été supprimé. Les types ont été déplacés :

| Ancien namespace | Nouveau namespace | Package |
| --- | --- | --- |
| `Granit.Abstractions.Domain` | `Granit.Core.Domain` | Core |
| `Granit.Abstractions.Timing` | `Granit.Timing` | Timing |
| `Granit.Abstractions.Guids` | `Granit.Guids` | Guids |
| `Granit.Abstractions.Security` | `Granit.Security` | Security |

### Guide de migration

1. Remplacer les `PackageReference` :

   ```xml
   <!-- Avant -->
   <PackageReference Include="Granit.Abstractions" />

   <!-- Après -->
   <PackageReference Include="Granit.Core" />
   ```

2. Mettre à jour les `using` :

   ```csharp
   // Avant
   using Granit.Abstractions.Domain;
   using Granit.Abstractions.Timing;
   using Granit.Abstractions.Guids;
   using Granit.Abstractions.Security;

   // Après
   using Granit.Core.Domain;
   using Granit.Timing;
   using Granit.Guids;
   using Granit.Security;
   ```

3. Remplacer les `AddGranit*()` individuels par `await builder.AddGranitAsync<T>()`
   (voir [modularity.md](modularity.md)).

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| ISO 27001 - Audit trail 3 ans | Hiérarchie `AuditedEntity` / `FullAuditedEntity` + `AuditLogEntry` |
| RGPD - Droit à l'oubli | `ISoftDeletable` (suppression logique) |
| ISO 27001 - Chiffrement au repos | `ITransitEncryptionService` (dans package Vault) |
| ISO 27001 - Traçabilité utilisateur | `ICurrentUserService` (dans package Security) |
| ISO 27001 - Horodatage UTC | `IClock` (dans package Timing) |

## Dépendances

### Packages NuGet

| Package | Rôle |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `IServiceCollection` |
| `Microsoft.Extensions.Hosting.Abstractions` | `IHostApplicationBuilder`, `IHost` |
| `Microsoft.Extensions.Options` | `IOptions<T>` |
| `Microsoft.AspNetCore.App` (FrameworkReference) | `IApplicationBuilder`, `WebApplication` |

### Dépendances Granit

`Granit.Core` est le **package racine** du framework — il ne dépend d'aucun autre
module Granit. Tous les modules Granit en dépendent directement ou transitivement.

| Direction | Modules |
|-----------|---------|
| **Dépend de** | Aucun module Granit |
| **Utilisé par (direct)** | `Timing`, `Guids`, `Security`, `ExceptionHandling`, `Validation`, `Caching`, `Encryption`, `Diagnostics`, `Observability`, `MultiTenancy`, `Persistence`, `Authorization`, `Wolverine`, `Localization`, `Features`, `BackgroundJobs`, `BlobStorage`, `Settings`, `Webhooks`, `Idempotency`, `ApiVersioning`, `Vault` |

> Voir le [graphe de dépendances complet](../dependencies.md).
