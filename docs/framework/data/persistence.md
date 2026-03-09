# Persistence

`Granit.Persistence` fournit les intercepteurs EF Core pour
l'audit trail HDS et la suppression logique RGPD.

## Installation

```bash
dotnet add package Granit.Persistence
```

## Configuration

### Avec le système de modules (recommandé)

`GranitPersistenceModule` déclare ses dépendances via `[DependsOn]` sur Timing,
Guids, Security et MultiTenancy. Il suffit d'utiliser `AddGranit<T>()` dans `Program.cs`
et les dépendances sont chargées automatiquement dans le bon ordre
(voir [modularity.md](../core/modularity.md)).

### Enregistrement direct

Pour les projets qui n'utilisent pas le système de modules, l'ordre d'appel est
important :

```csharp
builder.Services.AddGranitTiming();      // IClock (requis par les intercepteurs)
builder.Services.AddGranitGuids();       // IGuidGenerator (requis par AuditedEntityInterceptor)
builder.Services.AddGranitSecurity(); // ICurrentUserService
builder.Services.AddGranitMultiTenancy(); // ICurrentTenant (requis par AuditedEntityInterceptor)
builder.Services.AddGranitPersistence();
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

Pour les entités implémentant `IMultiTenant` (voir [core.md](../core/core.md#imultitenant)),
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

## VersioningInterceptor

Intercepteur `SaveChanges` qui assigne automatiquement `BusinessId` et `Version`
sur les entités implémentant `IVersioned` lors de l'insertion.

### Comportement du VersioningInterceptor

| État | Action |
| --- | --- |
| `EntityState.Added` | Assigne `BusinessId` (si `Guid.Empty`) et `Version` (max existant + 1) |
| `EntityState.Modified` | Aucune action — les mises à jour sont en place |

Le versionnement est **indépendant du workflow**. Trois cas d'usage :

1. **Versionnement pur** (ex : Patient) — `IVersioned` seul
2. **Workflow pur** (ex : Facture) — `IWorkflowStateful` seul
3. **Versionnement + Workflow** (ex : Document) — `VersionedWorkflowEntity`

### Interface IVersioned

```csharp
public interface IVersioned
{
    Guid BusinessId { get; set; }
    int Version { get; set; }
}
```

### Exemple de versionnement

```csharp
// Entité avec versionnement pur (pas de workflow)
public sealed class FichePatient : AuditedEntity, IVersioned
{
    public Guid BusinessId { get; set; }
    public int Version { get; set; }
    public string Nom { get; set; } = string.Empty;
}

// Première version : BusinessId et Version assignés automatiquement
db.Fiches.Add(new FichePatient { Nom = "Martin" });
await db.SaveChangesAsync();
// fiche.BusinessId == Guid généré automatiquement
// fiche.Version == 1

// Nouvelle version du même patient : même BusinessId
db.Fiches.Add(new FichePatient
{
    BusinessId = fiche.BusinessId,
    Nom = "Martin (mis à jour)",
});
await db.SaveChangesAsync();
// nouvelleFiche.Version == 2
```

> **Création de version explicite** : le `VersioningInterceptor` ne transforme jamais
> un `Modified` en `Added`. Créer une nouvelle version est toujours une opération
> explicite (ajouter une nouvelle entité avec le même `BusinessId`).

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

## Checklist — DbContext isolé (`*.EntityFrameworkCore`)

Chaque package Granit `*.EntityFrameworkCore` qui possède un `DbContext` isolé
**DOIT** respecter cette checklist sans exception :

1. **`<ProjectReference>` vers `Granit.Persistence`** dans le `.csproj`.
2. **Injection constructeur** de `ICurrentTenant?` (`Granit.Core.MultiTenancy`)
   et `IDataFilter?` (`Granit.Core.DataFiltering`), tous deux optionnels avec
   valeur par défaut `null`.
3. **Appel `modelBuilder.ApplyGranitConventions(currentTenant, dataFilter)`** à
   la fin de `OnModelCreating` — applique les query filters pour `ISoftDeletable`,
   `IMultiTenant`, `IActive`, `IProcessingRestrictable` et `IPublishable`.
4. **Câblage des intercepteurs** dans la méthode d'extension : utiliser la
   surcharge `(sp, options)` de `AddDbContextFactory` avec
   `ServiceLifetime.Scoped` et résoudre `AuditedEntityInterceptor` /
   `SoftDeleteInterceptor` depuis le service provider.
5. **`[DependsOn(typeof(GranitPersistenceModule))]`** sur la classe module.
6. **Pas de `HasQueryFilter` manuel** dans les configurations d'entité —
   `ApplyGranitConventions` gère tous les filtres standard centralement. Les
   filtres manuels causent des doublons ou des conflits (EF Core ne conserve
   que le dernier `HasQueryFilter` par entité).
7. **`IMultiTenant`** : les entités multi-tenant utilisent `Guid? TenantId`
   (jamais `string`). L'interface vit dans `Granit.Core.Domain`.

### Exemple complet

```csharp
// DbContext
internal sealed class MyDbContext(
    DbContextOptions<MyDbContext> options,
    ICurrentTenant? currentTenant = null,
    IDataFilter? dataFilter = null) : DbContext(options)
{
    public DbSet<MyEntity> Entities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new MyEntityConfiguration());
        modelBuilder.ApplyGranitConventions(currentTenant, dataFilter);
    }
}

// Extension method
public static IHostApplicationBuilder AddMyEntityFrameworkCore(
    this IHostApplicationBuilder builder,
    Action<DbContextOptionsBuilder> configure)
{
    builder.Services.AddDbContextFactory<MyDbContext>((sp, options) =>
    {
        configure(options);

        AuditedEntityInterceptor? auditInterceptor =
            sp.GetService<AuditedEntityInterceptor>();
        if (auditInterceptor is not null)
            options.AddInterceptors(auditInterceptor);

        SoftDeleteInterceptor? softDeleteInterceptor =
            sp.GetService<SoftDeleteInterceptor>();
        if (softDeleteInterceptor is not null)
            options.AddInterceptors(softDeleteInterceptor);
    }, ServiceLifetime.Scoped);

    // ... service registrations
    return builder;
}
```

## Query Filters

`ModelBuilderExtensions.ApplyGranitConventions()` applique automatiquement des
filtres globaux EF Core sur toutes les entités Granit détectées dans le modèle.

```csharp
public static ModelBuilder ApplyGranitConventions(
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
    modelBuilder.ApplyGranitConventions();
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
modelBuilder.ApplyGranitConventions();

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
        modelBuilder.ApplyGranitConventions(_currentTenant);
    }
}
```

Le filtre est évalué dynamiquement à chaque requête (closure sur l'instance
`ICurrentTenant`, dont `.Id` est un `AsyncLocal` réévalué par flux).

> **Important** : passer l'**instance** `ICurrentTenant` (pas sa valeur `.Id`).
> EF Core met en cache le modèle par type de DbContext — la closure doit référencer
> l'objet pour observer les changements de tenant entre requêtes.

Si `ApplyGranitConventions()` est appelé sans `ICurrentTenant`, seul le filtre
soft delete est appliqué (pas d'isolation multi-tenant).

### Bypass sélectif via IDataFilter

`IDataFilter` permet de désactiver un filtre individuel pour le flux async courant,
sans toucher aux autres filtres. Injecter `IDataFilter` dans le DbContext et le passer
à `ApplyGranitConventions` :

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
        modelBuilder.ApplyGranitConventions(_currentTenant, _dataFilter);
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

## Data Seeding

`Granit.Persistence` fournit un mécanisme de peuplement de données initiales
inspiré d'ABP Framework. Il permet à chaque module de contribuer ses données
référentielles (types de documents, rôles par défaut, configurations) au
démarrage de l'application.

### Activation

L'enregistrement est opt-in :

```csharp
builder.Services.AddGranitDataSeeding();
```

### IDataSeedContributor

Chaque module implémente `IDataSeedContributor` pour contribuer ses données
initiales. Les contributeurs doivent être **idempotents** (vérifier l'existence
avant insertion) :

```csharp
public sealed class AuthDataSeedContributor(AuthDbContext dbContext) : IDataSeedContributor
{
    public async Task SeedAsync(DataSeedContext context, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Roles.AnyAsync(cancellationToken))
        {
            return; // Déjà initialisé — idempotent
        }

        dbContext.Roles.Add(new Role { Name = "Admin" });
        dbContext.Roles.Add(new Role { Name = "User" });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

Enregistrer le contributeur dans le module :

```csharp
services.AddTransient<IDataSeedContributor, AuthDataSeedContributor>();
```

### DataSeedContext

Le contexte passé à chaque contributeur contient :

| Propriété | Type | Description |
| --- | --- | --- |
| `TenantId` | `Guid?` | Identifiant du tenant (`null` = contexte host-level) |
| `Properties` | `Dictionary<string, object?>` | Données arbitraires pour les contributeurs |

Accès via indexeur :

```csharp
DataSeedContext context = new(tenantId: myTenantId);
context["AdminEmail"] = "admin@example.com";

// Dans le contributeur :
string? email = context["AdminEmail"] as string;
```

### Comportement au démarrage

1. Le `DataSeedingHostedService` s'exécute au démarrage de l'application
2. Il crée un `DataSeedContext` host-level (`TenantId = null`)
3. Le `DataSeeder` crée un scope DI et résout tous les `IDataSeedContributor`
4. Les contributeurs sont exécutés **séquentiellement**
5. Si un contributeur échoue, l'erreur est loguée et les suivants continuent
6. Les exceptions ne bloquent **jamais** le démarrage de l'application

### Seeding multi-tenant

Le hosted service effectue un seeding host-level. Pour seeder les données
d'un tenant spécifique (ex : provisionnement), appeler `IDataSeeder`
directement :

```csharp
IDataSeeder seeder = serviceProvider.GetRequiredService<IDataSeeder>();
DataSeedContext context = new(tenantId: newTenantId);
await seeder.SeedAsync(context, cancellationToken);
```

## Architecture

```text
Granit.Persistence
├── DataSeeding/
│   ├── IDataSeedContributor.cs             (interface publique — contributeur)
│   ├── IDataSeeder.cs                      (interface publique — orchestrateur)
│   ├── DataSeedContext.cs                   (contexte : TenantId + Properties)
│   ├── DataSeeder.cs                       (interne : résolution DI + résilience)
│   └── DataSeedingHostedService.cs         (interne : IHostedService au démarrage)
├── Interceptors/
│   ├── AuditedEntityInterceptor.cs         (audit HDS : CreatedAt/By, ModifiedAt/By)
│   ├── VersioningInterceptor.cs            (versionnement : BusinessId, Version)
│   └── SoftDeleteInterceptor.cs            (soft delete RGPD : IsDeleted, DeletedAt/By)
└── Extensions/
    ├── ModelBuilderExtensions.cs            (ApplyGranitConventions : ISoftDeletable,
    │                                         IActive, IMultiTenant, IDataFilter bypass)
    └── PersistenceServiceCollectionExtensions.cs  (AddGranitPersistence, AddGranitDataSeeding)
```

## Services enregistrés

### AddGranitPersistence()

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `AuditedEntityInterceptor` | - | Scoped |
| `VersioningInterceptor` | - | Scoped |
| `SoftDeleteInterceptor` | - | Scoped |
| `IDataFilter` | `DataFilter` | Singleton |

### AddGranitDataSeeding() (opt-in)

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IDataSeeder` | `DataSeeder` | Singleton |
| `IHostedService` | `DataSeedingHostedService` | - |

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
currentTenant.IsAvailable.Returns(false);  // pas de tenant actif
currentTenant.Id.Returns((Guid?)null);

// Avec tenant actif :
// currentTenant.IsAvailable.Returns(true);
// currentTenant.Id.Returns((Guid?)tenantId);

AuditedEntityInterceptor interceptor = new(currentUser, clock, guidGenerator, currentTenant);
// ... assertions exactes avec Be() au lieu de BeCloseTo()
```

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| HDS - Audit trail | `AuditedEntityInterceptor` (CreatedAt/By, ModifiedAt/By) |
| HDS - Versionnement | `VersioningInterceptor` (BusinessId, Version — traçabilité des révisions) |
| HDS - Horodatage UTC | `IClock.Now` (jamais `DateTimeOffset.UtcNow`) |
| RGPD - Droit à l'oubli | `SoftDeleteInterceptor` (suppression logique) |
| RGPD - Minimisation | Query filters (entités supprimées et inactives exclues par défaut) |
| RGPD - Isolation tenant | Query filter multi-tenant (`ApplyGranitConventions(currentTenant)`) |
| RGPD - Pseudonymisation | `TenantId` GUID — jamais de données nominatives dans ce champ |
| Maintenance HDS | `IDataFilter.Disable<ISoftDeletable>()` — accès aux données supprimées en scope contrôlé |

## Isolation multi-tenant — patterns avancés

Pour aller au-delà du filtre `TenantId` partagé, Granit.Persistence propose des patterns
d'isolation physique :

- [Tenant-per-Database](isolation-tenant-per-database.md) — base de données dédiée par tenant
- [Tenant-per-Schema](isolation-tenant-per-schema.md) — schéma PostgreSQL dédié par tenant
- [Sélection de stratégie](isolation-strategie.md) — choisir statiquement ou dynamiquement le pattern

## Dépendances Granit

- **Dépend de** : `Granit.Core`, `Granit.Timing`, `Granit.Guids`, `Granit.Security`,
  `Granit.ExceptionHandling`
- **Utilisé par** : `Granit.Authorization.EntityFrameworkCore`,
  `Granit.Wolverine.Postgresql`, `Granit.Localization.EntityFrameworkCore`,
  `Granit.Features.EntityFrameworkCore`, `Granit.Settings.EntityFrameworkCore`,
  `Granit.Persistence.Migrations`

> **5 dépendances directes** — c'est le module avec le plus de dépendances dans le
> framework. Ce couplage est justifié : `Timing` fournit `IClock` pour l'horodatage
> HDS, `Guids` fournit les identifiants séquentiels, `Security` fournit
> `ICurrentUserService` pour l'audit trail, et `ExceptionHandling` fournit les
> exceptions métier pour les violations de contraintes.
>
> Voir le [graphe de dépendances complet](../dependencies.md).
