# Core

`DigitalDynamics.Foundation.Core` est le package fondation de tous les autres packages
Foundation. Il fournit :

- Le **système de modules** (voir [modularity.md](modularity.md)) : `FoundationModule`,
  `[DependsOn]`, `AddFoundationAsync<T>()`, tri topologique
- Les **types domaine partagés** : `AuditableEntity`, `ISoftDeletable`, `AuditLogEntry`

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

### AuditableEntity

Classe de base abstraite pour toutes les entités persistées nécessitant un audit trail
HDS. Les champs sont remplis automatiquement par `AuditableEntityInterceptor` du package
[Persistence](persistence.md).

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public abstract class AuditableEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
```

Toute entité persistée **doit** hériter de cette classe pour garantir la traçabilité HDS.

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public sealed class Patient : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
```

### ISoftDeletable

Interface pour la suppression logique conforme au RGPD. Les entités contenant des
données personnelles implémentent cette interface pour supporter le droit à l'oubli
via suppression logique.

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

```csharp
using DigitalDynamics.Foundation.Core.Domain;

public sealed class Patient : AuditableEntity, ISoftDeletable
{
    public string FirstName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

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
│   ├── AuditableEntity.cs          (classe de base, audit trail HDS)
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
| HDS - Audit trail 3 ans | `AuditableEntity` + `AuditLogEntry` |
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
