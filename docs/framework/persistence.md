# Persistence

`DigitalDynamics.Foundation.Persistence` fournit les intercepteurs EF Core pour
l'audit trail HDS et la suppression logique RGPD.

## Installation

```bash
dotnet add package DigitalDynamics.Foundation.Persistence
```

## Configuration

### Avec le système de modules (recommandé)

`FoundationPersistenceModule` déclare ses dépendances via `[DependsOn]` sur Timing,
Guids et Security. Il suffit d'utiliser `AddFoundation<T>()` dans `Program.cs` et les
dépendances sont chargées automatiquement dans le bon ordre
(voir [modularity.md](modularity.md)).

### Enregistrement direct

Pour les projets qui n'utilisent pas le système de modules, l'ordre d'appel est
important :

```csharp
builder.Services.AddFoundationTiming();      // IClock (requis par les intercepteurs)
builder.Services.AddFoundationGuids();       // IGuidGenerator (requis par AuditableEntityInterceptor)
builder.Services.AddFoundationSecurity(builder.Configuration); // ICurrentUserService
builder.Services.AddFoundationPersistence();
```

## AuditableEntityInterceptor

Intercepteur `SaveChanges` qui remplit automatiquement les champs d'audit sur les
entités `AuditableEntity`.

### Comportement

| État | Champs remplis |
| --- | --- |
| `EntityState.Added` | `CreatedAt`, `CreatedBy`, `Id` (si `Guid.Empty`) |
| `EntityState.Modified` | `ModifiedAt`, `ModifiedBy` |

Les champs `CreatedAt` et `CreatedBy` sont protégés contre la modification lors
d'un `UPDATE` (via `IsModified = false`).

### Sources des valeurs

- **Horodatage** : `IClock.Now` (UTC garanti)
- **Utilisateur** : `ICurrentUserService.UserId` (fallback : `"system"`)

### Exemple

```csharp
// L'entité hérite de AuditableEntity
public sealed class Patient : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
}

// À l'ajout, les champs sont remplis automatiquement
db.Patients.Add(new Patient { FirstName = "Jean" });
await db.SaveChangesAsync();
// patient.CreatedAt == clock.Now (UTC)
// patient.CreatedBy == "user-123" (depuis le JWT)
// patient.Id == Guid généré automatiquement
```

## SoftDeleteInterceptor

Intercepteur `SaveChanges` qui convertit les suppressions physiques en suppressions
logiques pour les entités `ISoftDeletable`.

### Comportement du SoftDeleteInterceptor

Quand une entité `ISoftDeletable` est marquée `EntityState.Deleted` :

1. L'état est changé en `EntityState.Modified`
2. `IsDeleted` est positionné à `true`
3. `DeletedAt` est positionné à `IClock.Now` (UTC)
4. `DeletedBy` est positionné à `ICurrentUserService.UserId`

L'entité reste en base de données pour l'audit trail HDS (3 ans de rétention).

### Exemple de suppression logique

```csharp
// Suppression standard
db.Patients.Remove(patient);
await db.SaveChangesAsync();

// Résultat : pas de DELETE SQL, mais un UPDATE
// patient.IsDeleted == true
// patient.DeletedAt == clock.Now
// patient.DeletedBy == "user-123"
```

## Query Filters (Soft Delete)

Le `ModelBuilderExtensions.ApplyFoundationConventions()` applique un filtre global
sur toutes les entités `ISoftDeletable` :

```csharp
// Dans le DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyFoundationConventions();
}
```

Ce filtre ajoute automatiquement `WHERE IsDeleted = false` à toutes les requêtes.
Pour inclure les entités supprimées (ex : audit), utiliser `IgnoreQueryFilters()` :

```csharp
var allPatients = await db.Patients
    .IgnoreQueryFilters()
    .ToListAsync();
```

## Architecture

```text
DigitalDynamics.Foundation.Persistence
├── Interceptors/
│   ├── AuditableEntityInterceptor.cs     (audit HDS : CreatedAt/By, ModifiedAt/By)
│   └── SoftDeleteInterceptor.cs          (soft delete RGPD : IsDeleted, DeletedAt/By)
└── Extensions/
    ├── ModelBuilderExtensions.cs          (ApplyFoundationConventions : query filters)
    └── PersistenceServiceCollectionExtensions.cs  (AddFoundationPersistence)
```

## Services enregistrés

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `AuditableEntityInterceptor` | - | Scoped |
| `SoftDeleteInterceptor` | - | Scoped |

## Tests

Les intercepteurs sont testables grâce à l'injection d'`IClock` :

```csharp
var clock = Substitute.For<IClock>();
var fixedNow = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
clock.Now.Returns(fixedNow);

var interceptor = new AuditableEntityInterceptor(currentUserService, clock);
// ... assertions exactes avec Be() au lieu de BeCloseTo()
```

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| HDS - Audit trail | `AuditableEntityInterceptor` (CreatedAt/By, ModifiedAt/By) |
| HDS - Horodatage UTC | `IClock.Now` (jamais `DateTimeOffset.UtcNow`) |
| RGPD - Droit à l'oubli | `SoftDeleteInterceptor` (suppression logique) |
| RGPD - Minimisation | Query filters (entités supprimées exclues par défaut) |
