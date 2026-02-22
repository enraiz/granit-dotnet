# Core

`DigitalDynamics.Foundation.Core` est le package fondation de tous les autres packages
Foundation. Il fournit :

- Le **système de modules** (voir [modularity.md](modularity.md)) : `FoundationModule`,
  `[DependsOn]`, `AddFoundationAsync<T>()`, tri topologique
- Les **types domaine partagés** (voir [domain.md](../data/domain.md)) : hiérarchie d'entités
  (`Entity`, `CreationAuditedEntity`, `AuditedEntity`, `FullAuditedEntity`),
  `ISoftDeletable`, `IMultiTenant`, `IActive`, `AuditLogEntry`
- Le **service de data filtering** (voir [data-filtering.md](../data/data-filtering.md)) :
  `IDataFilter` — bypass sélectif des query filters globaux EF Core

Ce package remplace l'ancien `Foundation.Abstractions`. Les interfaces de service
(`IClock`, `IGuidGenerator`, `ICurrentUserService`, `ITransitEncryptionService`) vivent
désormais dans leurs modules respectifs (voir section
[Migration depuis Abstractions](#migration-depuis-abstractions)).

## Installation

```bash
dotnet add package DigitalDynamics.Foundation.Core
```

Ce package est automatiquement tiré comme dépendance transitive par tous les packages
Foundation. Il n'est nécessaire de le référencer explicitement que dans les projets qui
utilisent les types domaine sans autre package Foundation.

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
DigitalDynamics.Foundation.Core
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
├── Modularity/
│   ├── FoundationModule.cs         (classe de base des modules)
│   ├── DependsOnAttribute.cs       (déclaration de dépendances)
│   ├── ServiceConfigurationContext.cs
│   ├── ApplicationInitializationContext.cs
│   ├── ModuleDescriptor.cs         (internal)
│   ├── ModuleLoader.cs             (internal, tri topologique)
│   └── FoundationApplication.cs    (singleton, orchestrateur lifecycle)
└── Extensions/
    ├── FoundationHostBuilderExtensions.cs   (AddFoundation<T> / AddFoundationAsync<T>)
    └── FoundationApplicationExtensions.cs   (UseFoundation / UseFoundationAsync)
```

## Migration depuis Abstractions

`Foundation.Abstractions` a été supprimé. Les types ont été déplacés :

| Ancien namespace | Nouveau namespace | Package |
| --- | --- | --- |
| `DigitalDynamics.Foundation.Abstractions.Domain` | `DigitalDynamics.Foundation.Core.Domain` | Core |
| `DigitalDynamics.Foundation.Abstractions.Timing` | `DigitalDynamics.Foundation.Timing` | Timing |
| `DigitalDynamics.Foundation.Abstractions.Guids` | `DigitalDynamics.Foundation.Guids` | Guids |
| `DigitalDynamics.Foundation.Abstractions.Security` | `DigitalDynamics.Foundation.Security` | Security |

### Guide de migration

1. Remplacer les `PackageReference` :

   ```xml
   <!-- Avant -->
   <PackageReference Include="DigitalDynamics.Foundation.Abstractions" />

   <!-- Après -->
   <PackageReference Include="DigitalDynamics.Foundation.Core" />
   ```

2. Mettre à jour les `using` :

   ```csharp
   // Avant
   using DigitalDynamics.Foundation.Abstractions.Domain;
   using DigitalDynamics.Foundation.Abstractions.Timing;
   using DigitalDynamics.Foundation.Abstractions.Guids;
   using DigitalDynamics.Foundation.Abstractions.Security;

   // Après
   using DigitalDynamics.Foundation.Core.Domain;
   using DigitalDynamics.Foundation.Timing;
   using DigitalDynamics.Foundation.Guids;
   using DigitalDynamics.Foundation.Security;
   ```

3. Remplacer les `AddFoundation*()` individuels par `await builder.AddFoundationAsync<T>()`
   (voir [modularity.md](modularity.md)).

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| HDS - Audit trail 3 ans | Hiérarchie `AuditedEntity` / `FullAuditedEntity` + `AuditLogEntry` |
| RGPD - Droit à l'oubli | `ISoftDeletable` (suppression logique) |
| HDS - Chiffrement au repos | `ITransitEncryptionService` (dans package Vault) |
| HDS - Traçabilité utilisateur | `ICurrentUserService` (dans package Security) |
| HDS - Horodatage UTC | `IClock` (dans package Timing) |

## Dépendances

| Package | Rôle |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `IServiceCollection` |
| `Microsoft.Extensions.Hosting.Abstractions` | `IHostApplicationBuilder`, `IHost` |
| `Microsoft.Extensions.Options` | `IOptions<T>` |
| `Microsoft.AspNetCore.App` (FrameworkReference) | `IApplicationBuilder`, `WebApplication` |
