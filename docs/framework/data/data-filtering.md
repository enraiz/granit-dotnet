# Data Filtering

`IDataFilter` permet de désactiver individuellement un query filter global EF Core
pour le flux async courant, sans appeler `IgnoreQueryFilters()` qui désactiverait
tous les filtres simultanément.

```csharp
using Granit.Core.DataFiltering;
```

## IDataFilter

```csharp
public interface IDataFilter
{
    IDisposable Disable<TFilter>() where TFilter : class;
    IDisposable Enable<TFilter>() where TFilter : class;
    bool IsEnabled<TFilter>() where TFilter : class;
}
```

Enregistré comme **Singleton** par `AddGranitPersistence()`. L'état est stocké
dans un `static AsyncLocal` — chaque flux async a son propre état indépendant, isolé
des autres requêtes en cours (identique au pattern `ICurrentTenant`).

## Utilisation

```csharp
// Désactiver le soft delete pour ce scope (job de purge, vue admin, audit trail)
using IDisposable scope = _dataFilter.Disable<ISoftDeletable>();
List<DossierPatient> tousLesDossiers = await _context.Dossiers.ToListAsync(ct);
// Filtre restauré automatiquement à la sortie du using

// Désactiver le filtre multi-tenant (opération système cross-tenant)
using IDisposable scope = _dataFilter.Disable<IMultiTenant>();
int totalGlobal = await _context.Dossiers.CountAsync(ct);

// Désactiver le filtre IActive (inclure les entités désactivées)
using IDisposable scope = _dataFilter.Disable<IActive>();
List<Etablissement> tous = await _context.Etablissements.ToListAsync(ct);
```

## Scopes imbriqués

Les scopes sont **imbriquables** et **restaurent l'état précédent** à `Dispose()` :

```csharp
// Niveau 0 : tous les filtres actifs (état par défaut)

using IDisposable disableScope = _dataFilter.Disable<ISoftDeletable>();
// Niveau 1 : soft delete désactivé

using IDisposable reEnableScope = _dataFilter.Enable<ISoftDeletable>();
// Niveau 2 : soft delete réactivé dans ce sous-scope

// reEnableScope.Dispose() → retour niveau 1 : soft delete désactivé
// disableScope.Dispose() → retour niveau 0 : soft delete actif
```

## Injection dans le DbContext

Pour activer le bypass, injecter `IDataFilter` dans le DbContext et le passer à
`ApplyGranitConventions` :

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

> **Rétrocompatibilité** : si `dataFilter` n'est pas passé (ou est `null`), tous les
> filtres sont toujours appliqués. Les DbContexts existants n'ont pas besoin d'être
> modifiés.

## Exemple : service de purge HDS

```csharp
public sealed class PurgeService(IDataFilter dataFilter, AppDbContext context)
{
    public async Task PurgerDossiersSupprimes(CancellationToken cancellationToken)
    {
        using IDisposable scope = dataFilter.Disable<ISoftDeletable>();
        List<DossierPatient> aSupprimer = await context.Dossiers
            .Where(d => d.IsDeleted && d.DeletedAt < DateTimeOffset.UtcNow.AddYears(-3))
            .ToListAsync(ct);
        context.Dossiers.RemoveRange(aSupprimer);
        await context.SaveChangesAsync(ct);
    }
}
```

## Sécurité

> **`Disable<IMultiTenant>()`** désactive l'isolation par tenant.
> Réserver aux opérations système explicitement autorisées (migrations, jobs de
> maintenance). Ne jamais exposer ce bypass dans des endpoints appelables par un
> utilisateur final.

## Architecture

```text
Granit.Core
└── DataFiltering/
    ├── IDataFilter.cs    (interface : Disable<T>, Enable<T>, IsEnabled<T>)
    └── DataFilter.cs     (implémentation AsyncLocal<ImmutableDictionary<Type, bool>>)
```

### Implémentation AsyncLocal

`DataFilter` stocke l'état dans un `static readonly AsyncLocal<ImmutableDictionary<Type, bool>>`.

- Absence d'une clé = filtre activé (état par défaut)
- `SetItem` (copy-on-write) à chaque mutation → pas de trap de mutation enfant
- `FilterScope` interne restaure l'état précédent à `Dispose()`

Ce pattern est identique à `CurrentTenant` (package MultiTenancy).

## Enregistrement DI

| Service | Implémentation | Lifetime | Enregistré par |
| --- | --- | --- | --- |
| `IDataFilter` | `DataFilter` | Singleton | `AddGranitPersistence()` |

`IDataFilter` est **défini dans Core** (zéro dépendance externe) mais **enregistré par
Persistence**. Les projets qui utilisent Core sans Persistence peuvent injecter leur
propre implémentation.

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| Maintenance HDS | `Disable<ISoftDeletable>()` — accès aux données supprimées en scope contrôlé |
| Isolation tenant | `Disable<IMultiTenant>()` — réservé aux opérations système autorisées |
