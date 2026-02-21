# Core

`DigitalDynamics.Foundation.Core` est le package fondation de tous les autres packages
Foundation. Il fournit :

- Le **système de modules** (voir [modularity.md](modularity.md)) : `FoundationModule`,
  `[DependsOn]`, `AddFoundationAsync<T>()`, tri topologique
- Les **types domaine partagés** : hiérarchie d'entités (`Entity`, `CreationAuditedEntity`,
  `AuditedEntity`, `FullAuditedEntity`), `ISoftDeletable`, `AuditLogEntry`

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

### Hiérarchie d'entités

Les entités persistées héritent d'une hiérarchie de classes abstraites qui ajoute
progressivement les champs d'audit HDS. Les champs sont remplis automatiquement par
`AuditedEntityInterceptor` du package [Persistence](persistence.md).

```text
Entity (abstract)
└── CreationAuditedEntity (abstract)
    └── AuditedEntity (abstract)
        └── FullAuditedEntity (abstract, implémente ISoftDeletable)
```

Choisir le niveau approprié selon les besoins de traçabilité de l'entité :

| Classe | Champs ajoutés | Usage |
| --- | --- | --- |
| `Entity` | `Id` | Entité de base sans audit |
| `CreationAuditedEntity` | `CreatedAt`, `CreatedBy` | Traçabilité de création uniquement |
| `AuditedEntity` | `ModifiedAt`, `ModifiedBy` | Traçabilité de création et modification |
| `FullAuditedEntity` | `IsDeleted`, `DeletedAt`, `DeletedBy` (via `ISoftDeletable`) | Audit complet + suppression logique RGPD |

### Entity

Classe de base abstraite minimale pour toutes les entités persistées. Fournit
uniquement l'identifiant.

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public abstract class Entity
{
    public Guid Id { get; set; }
}
```

### CreationAuditedEntity

Ajoute les champs de traçabilité de création. Pour les entités qui n'ont pas besoin
de tracer les modifications.

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public abstract class CreationAuditedEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

### AuditedEntity

Ajoute les champs de traçabilité de modification. C'est le choix par défaut pour la
plupart des entités nécessitant un audit trail HDS.

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public abstract class AuditedEntity : CreationAuditedEntity
{
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
```

Toute entité persistée nécessitant un audit trail complet **doit** hériter de cette
classe (ou de `FullAuditedEntity`) pour garantir la traçabilité HDS.

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public sealed class Patient : AuditedEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
```

### FullAuditedEntity

Ajoute la suppression logique conforme au RGPD en implémentant `ISoftDeletable`. Pour
les entités contenant des données personnelles qui doivent supporter le droit à l'oubli.

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public abstract class FullAuditedEntity : AuditedEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public sealed class Patient : FullAuditedEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
```

### ISoftDeletable

Interface pour la suppression logique conforme au RGPD. `FullAuditedEntity` implémente
cette interface. Les entités qui ne s'inscrivent pas dans la hiérarchie standard peuvent
implémenter `ISoftDeletable` directement.

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

Le `SoftDeleteInterceptor` (package [Persistence](persistence.md)) transforme les
opérations `DELETE` en `UPDATE SET IsDeleted = true`, conservant les données pour
l'audit trail tout en les excluant des requêtes standard.

### AuditLogEntry

Classe scellée représentant une entrée de l'audit trail HDS. Enregistre qui a fait
quoi, quand et sur quelle entité.

```csharp
public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string UserId { get; set; }
    public string Operation { get; set; }       // Create, Update, Delete, SoftDelete
    public string EntityType { get; set; }       // Type CLR de l'entité
    public string EntityId { get; set; }
    public string? Changes { get; set; }         // JSON des propriétés modifiées
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

Conformité HDS : les entrées d'audit sont conservées 3 ans.

## Architecture

```text
DigitalDynamics.Foundation.Core
├── Domain/
│   ├── Entity.cs                   (classe de base, identifiant)
│   ├── CreationAuditedEntity.cs    (+ CreatedAt, CreatedBy)
│   ├── AuditedEntity.cs            (+ ModifiedAt, ModifiedBy)
│   ├── FullAuditedEntity.cs        (+ ISoftDeletable)
│   ├── ISoftDeletable.cs           (suppression logique RGPD)
│   └── AuditLogEntry.cs            (entrée d'audit trail)
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
