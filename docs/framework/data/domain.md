# Types domaine

`Granit.Core.Domain` fournit la hiérarchie d'entités persistées
et les interfaces domaine partagées par tous les packages Granit.

```csharp
using Granit.Core.Domain;
```

## Hiérarchie d'entités

Les entités persistées héritent d'une hiérarchie de classes abstraites qui ajoute
progressivement les champs d'audit ISO 27001. Les champs sont remplis automatiquement par
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
using Granit.Core.Domain;

public abstract class Entity
{
    public Guid Id { get; set; }
}
```

### CreationAuditedEntity

Ajoute les champs de traçabilité de création. Pour les entités qui n'ont pas besoin
de tracer les modifications.

```csharp
using Granit.Core.Domain;

public abstract class CreationAuditedEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

### AuditedEntity

Ajoute les champs de traçabilité de modification. C'est le choix par défaut pour la
plupart des entités nécessitant un audit trail ISO 27001.

```csharp
using Granit.Core.Domain;

public abstract class AuditedEntity : CreationAuditedEntity
{
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
```

Toute entité persistée nécessitant un audit trail complet **doit** hériter de cette
classe (ou de `FullAuditedEntity`) pour garantir la traçabilité ISO 27001.

```csharp
using Granit.Core.Domain;

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
using Granit.Core.Domain;

public abstract class FullAuditedEntity : AuditedEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

```csharp
using Granit.Core.Domain;

public sealed class Patient : FullAuditedEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
```

## ISoftDeletable

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

## IMultiTenant

Interface pour les entités dont les données sont isolées par tenant. Le `TenantId` est
automatiquement rempli par `AuditedEntityInterceptor` (package [Persistence](persistence.md))
lors de la création, depuis `ICurrentTenant` du tenant courant.

```csharp
public interface IMultiTenant
{
    Guid? TenantId { get; set; }
}
```

Utilisation typique — combiner avec la hiérarchie d'entités :

```csharp
using Granit.Core.Domain;

// Entité dont chaque enregistrement appartient à un tenant
public sealed class DossierPatient : FullAuditedEntity, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string NumeroAdmission { get; set; } = string.Empty;
}
```

Comportement automatique à la création (`SaveChangesAsync`) :

- Si `TenantId == null` et qu'un tenant est actif → `TenantId = ICurrentTenant.Id`
- Si `TenantId` est déjà défini (migration, import) → valeur conservée
- Si aucun tenant actif → `TenantId` reste `null` (donnée globale)

Le query filter multi-tenant (`WHERE TenantId = currentTenant.Id`) est activé en
passant l'`ICurrentTenant` à `ApplyGranitConventions` dans `OnModelCreating`
(voir [persistence.md](persistence.md#query-filters)).

**Conformité RGPD** : `TenantId` est un GUID pseudonymisé. Ne jamais stocker de
données nominatives (nom, email) dans ce champ.

## IActive

Interface pour les entités avec un statut actif/inactif. Les entités `IsActive = false`
sont exclues des requêtes standard par un query filter global (`WHERE IsActive = true`).

```csharp
public interface IActive
{
    bool IsActive { get; set; }
}
```

Utilisation typique :

```csharp
using Granit.Core.Domain;

public sealed class Etablissement : AuditedEntity, IActive
{
    public bool IsActive { get; set; } = true;
    public string Nom { get; set; } = string.Empty;
}
```

Le query filter `IActive` est activé automatiquement par `ApplyGranitConventions`
(voir [persistence.md](persistence.md#query-filter-iactive)). Il peut être désactivé
ponctuellement via `IDataFilter` (voir [data-filtering.md](data-filtering.md)).

## ITranslatable / ITranslation

Interfaces pour les entités dont les propriétés textuelles sont stockées en plusieurs
langues via une **table de traduction par entité**.

```csharp
public interface ITranslatable<TTranslation>
{
    ICollection<TTranslation> Translations { get; }
}

public interface ITranslation
{
    Guid ParentId { get; set; }
    string Culture { get; set; }  // BCP 47
}
```

Deux classes de base sont fournies :

| Classe | Hérite de | Usage |
| --- | --- | --- |
| `Translation<TParent>` | `Entity` | Traduction sans audit |
| `AuditedTranslation<TParent>` | `AuditedEntity` | Traduction avec audit ISO 27001 |

Exemple :

```csharp
using Granit.Core.Domain;

public sealed class Document : AuditedEntity, ITranslatable<DocumentTranslation>
{
    public string InternalCode { get; set; } = string.Empty;
    public ICollection<DocumentTranslation> Translations { get; set; } = [];
}

public sealed class DocumentTranslation : AuditedTranslation<Document>
{
    public string Title { get; set; } = string.Empty;
}
```

La configuration EF Core (FK, cascade delete, index unique) est appliquée
automatiquement par `ApplyGranitConventions()`.

Voir [translations.md](translations.md) pour la documentation complète.

## AuditLogEntry

Classe scellée représentant une entrée de l'audit trail ISO 27001. Enregistre qui a fait
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

Conformité ISO 27001 : les entrées d'audit sont conservées 3 ans.

## Architecture

```text
Granit.Core
└── Domain/
    ├── Entity.cs                   (classe de base, identifiant)
    ├── CreationAuditedEntity.cs    (+ CreatedAt, CreatedBy)
    ├── AuditedEntity.cs            (+ ModifiedAt, ModifiedBy)
    ├── FullAuditedEntity.cs        (+ ISoftDeletable)
    ├── ISoftDeletable.cs           (suppression logique RGPD)
    ├── IMultiTenant.cs             (isolation par tenant, TenantId auto-injecté)
    ├── IActive.cs                  (filtre actif/inactif, WHERE IsActive = true)
    ├── ITranslatable.cs            (entité parente traduisible)
    ├── ITranslation.cs             (interface de traduction, Culture BCP 47)
    ├── Translation.cs              (classe de base traduction, hérite Entity)
    ├── AuditedTranslation.cs       (classe de base traduction ISO 27001, hérite AuditedEntity)
    ├── TranslatableExtensions.cs   (résolution in-memory, fallback / strict)
    └── AuditLogEntry.cs            (entrée d'audit trail)
```

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| ISO 27001 - Audit trail 3 ans | Hiérarchie `AuditedEntity` / `FullAuditedEntity` + `AuditLogEntry` |
| RGPD - Droit à l'oubli | `ISoftDeletable` (suppression logique) |
| RGPD - Isolation tenant | `IMultiTenant` + query filter (`ApplyGranitConventions`) |
| RGPD - Pseudonymisation | `TenantId` GUID — jamais de données nominatives dans ce champ |
