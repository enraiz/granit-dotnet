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
Guids, Security et MultiTenancy. Il suffit d'utiliser `AddFoundation<T>()` dans `Program.cs`
et les dépendances sont chargées automatiquement dans le bon ordre
(voir [modularity.md](modularity.md)).

### Enregistrement direct

Pour les projets qui n'utilisent pas le système de modules, l'ordre d'appel est
important :

```csharp
builder.Services.AddFoundationTiming();      // IClock (requis par les intercepteurs)
builder.Services.AddFoundationGuids();       // IGuidGenerator (requis par AuditedEntityInterceptor)
builder.Services.AddFoundationSecurity(builder.Configuration); // ICurrentUserService
builder.Services.AddFoundationMultiTenancy(builder.Configuration); // ICurrentTenant (requis par AuditedEntityInterceptor)
builder.Services.AddFoundationPersistence();
```

## AuditedEntityInterceptor

Intercepteur `SaveChanges` qui remplit automatiquement les champs d'audit sur les
entités héritant de la hiérarchie `CreationAuditedEntity` / `AuditedEntity` /
`FullAuditedEntity`.

### Comportement

| État | Champs remplis |
| --- | --- |
| `EntityState.Added` | `CreatedAt`, `CreatedBy`, `Id` (si `Guid.Empty`), `TenantId` (si `IMultiTenant`) |
| `EntityState.Modified` | `ModifiedAt`, `ModifiedBy` |

Les champs `CreatedAt` et `CreatedBy` sont protégés contre la modification lors
d'un `UPDATE` (via `IsModified = false`).

### Injection automatique du TenantId

Pour les entités implémentant `IMultiTenant` (voir [core.md](core.md#imultitenant)),
l'intercepteur injecte automatiquement le `TenantId` lors de la création :

- Si `TenantId == null` et qu'un tenant est actif → `TenantId = ICurrentTenant.Id`
- Si `TenantId` est déjà défini (migration, import) → valeur conservée
- Si aucun tenant actif (contexte système) → `TenantId` reste `null`

```csharp
public sealed class DossierPatient : FullAuditedEntity, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string NumeroAdmission { get; set; } = string.Empty;
}

// À l'ajout, TenantId est rempli depuis le contexte courant
db.Dossiers.Add(new DossierPatient { NumeroAdmission = "ADM-001" });
await db.SaveChangesAsync();
// dossier.TenantId == currentTenant.Id (si tenant actif)
```

### Sources des valeurs

- **Horodatage** : `IClock.Now` (UTC garanti)
- **Utilisateur** : `ICurrentUserService.UserId` (fallback : `"system"`)
- **Tenant** : `ICurrentTenant.Id` (null si pas de tenant actif)

### Exemple

```csharp
// L'entité hérite de AuditedEntity
public sealed class Patient : AuditedEntity
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

## Query Filters

`ModelBuilderExtensions.ApplyFoundationConventions()` applique automatiquement des
filtres globaux EF Core sur toutes les entités Foundation détectées dans le modèle.

```csharp
public static ModelBuilder ApplyFoundationConventions(
    this ModelBuilder modelBuilder,
    ICurrentTenant? currentTenant = null,
    IDataFilter? dataFilter = null)
```

| Interface | Filtre appliqué | Requis |
| --- | --- | --- |
| `ISoftDeletable` | `WHERE IsDeleted = false` | toujours |
| `IActive` | `WHERE IsActive = true` | toujours |
| `IMultiTenant` | `WHERE TenantId = currentTenant.Id` | si `currentTenant` fourni |

Les entités implémentant plusieurs interfaces reçoivent un **unique** `HasQueryFilter`
combinant les conditions avec `AND` (le problème de double `HasQueryFilter` — qui faisait
silencieusement perdre le premier filtre — est résolu depuis cette version).

### Query Filter Soft Delete

Toutes les entités `ISoftDeletable` reçoivent un filtre `WHERE IsDeleted = false` :

```csharp
// Dans le DbContext — filtre soft delete uniquement
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyFoundationConventions();
}
```

Pour inclure les entités supprimées (ex : audit trail), utiliser `IgnoreQueryFilters()`
(désactive tous les filtres) ou `IDataFilter.Disable<ISoftDeletable>()` (bypass sélectif) :

```csharp
// Tous les filtres désactivés
List<DossierPatient> tous = await db.Dossiers.IgnoreQueryFilters().ToListAsync();

// Soft delete seul désactivé (multi-tenant et IActive restent actifs)
using IDisposable scope = _dataFilter.Disable<ISoftDeletable>();
List<DossierPatient> tousAvecFiltres = await db.Dossiers.ToListAsync();
```

### Query Filter IActive

Toutes les entités `IActive` reçoivent un filtre `WHERE IsActive = true` :

```csharp
public sealed class Etablissement : AuditedEntity, IActive
{
    public bool IsActive { get; set; } = true;
    public string Nom { get; set; } = string.Empty;
}

// Dans OnModelCreating — filtre IActive appliqué automatiquement
modelBuilder.ApplyFoundationConventions();

// Seuls les établissements actifs sont retournés
List<Etablissement> actifs = await db.Etablissements.ToListAsync();
```

### Query Filter Multi-Tenant

Pour activer l'isolation automatique par tenant (`WHERE TenantId = currentTenant.Id`),
passer l'`ICurrentTenant` injecté dans le constructeur du DbContext :

```csharp
public sealed class AppDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyFoundationConventions(_currentTenant);
    }
}
```

Le filtre est évalué dynamiquement à chaque requête (closure sur l'instance
`ICurrentTenant`, dont `.Id` est un `AsyncLocal` réévalué par flux).

> **Important** : passer l'**instance** `ICurrentTenant` (pas sa valeur `.Id`).
> EF Core met en cache le modèle par type de DbContext — la closure doit référencer
> l'objet pour observer les changements de tenant entre requêtes.

Si `ApplyFoundationConventions()` est appelé sans `ICurrentTenant`, seul le filtre
soft delete est appliqué (pas d'isolation multi-tenant).

### Bypass sélectif via IDataFilter

`IDataFilter` permet de désactiver un filtre individuel pour le flux async courant,
sans toucher aux autres filtres. Injecter `IDataFilter` dans le DbContext et le passer
à `ApplyFoundationConventions` :

```csharp
public sealed class AppDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IDataFilter _dataFilter;

    public AppDbContext(
        DbContextOptions options,
        ICurrentTenant currentTenant,
        IDataFilter dataFilter)
        : base(options)
    {
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyFoundationConventions(_currentTenant, _dataFilter);
    }
}
```

Usage — désactiver un filtre dans un scope applicatif :

```csharp
// Job de purge HDS : accès aux enregistrements supprimés logiquement
using IDisposable scope = _dataFilter.Disable<ISoftDeletable>();
List<DossierPatient> aArchiver = await _context.Dossiers
    .Where(d => d.IsDeleted)
    .ToListAsync(ct);
// Filtre soft delete restauré automatiquement à la fin du using

// Requête système cross-tenant (opération de maintenance)
using IDisposable scope = _dataFilter.Disable<IMultiTenant>();
int totalGlobal = await _context.Dossiers.CountAsync(ct);
```

> **Rétrocompatibilité** : si `dataFilter` n'est pas passé (ou est `null`), tous les
> filtres sont toujours appliqués — comportement identique aux versions précédentes.
> Les DbContexts existants n'ont pas besoin d'être modifiés.

Pour la documentation complète de `IDataFilter`, voir [data-filtering.md](data-filtering.md).

## Architecture

```text
DigitalDynamics.Foundation.Persistence
├── Interceptors/
│   ├── AuditedEntityInterceptor.cs       (audit HDS : CreatedAt/By, ModifiedAt/By)
│   └── SoftDeleteInterceptor.cs          (soft delete RGPD : IsDeleted, DeletedAt/By)
└── Extensions/
    ├── ModelBuilderExtensions.cs          (ApplyFoundationConventions : ISoftDeletable,
    │                                       IActive, IMultiTenant, IDataFilter bypass)
    └── PersistenceServiceCollectionExtensions.cs  (AddFoundationPersistence)
```

## Services enregistrés

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `AuditedEntityInterceptor` | - | Scoped |
| `SoftDeleteInterceptor` | - | Scoped |
| `IDataFilter` | `DataFilter` | Singleton |

## Tests

Les intercepteurs sont testables grâce à l'injection de leurs dépendances :

```csharp
IClock clock = Substitute.For<IClock>();
DateTimeOffset fixedNow = new(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
clock.Now.Returns(fixedNow);

ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
currentUser.UserId.Returns("user-test-123");

IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
guidGenerator.Create().Returns(Guid.NewGuid());

ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
currentTenant.Id.Returns((Guid?)null);

AuditedEntityInterceptor interceptor = new(currentUser, clock, guidGenerator, currentTenant);
// ... assertions exactes avec Be() au lieu de BeCloseTo()
```

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| HDS - Audit trail | `AuditedEntityInterceptor` (CreatedAt/By, ModifiedAt/By) |
| HDS - Horodatage UTC | `IClock.Now` (jamais `DateTimeOffset.UtcNow`) |
| RGPD - Droit à l'oubli | `SoftDeleteInterceptor` (suppression logique) |
| RGPD - Minimisation | Query filters (entités supprimées et inactives exclues par défaut) |
| RGPD - Isolation tenant | Query filter multi-tenant (`ApplyFoundationConventions(currentTenant)`) |
| RGPD - Pseudonymisation | `TenantId` GUID — jamais de données nominatives dans ce champ |
| Maintenance HDS | `IDataFilter.Disable<ISoftDeletable>()` — accès aux données supprimées en scope contrôlé |
