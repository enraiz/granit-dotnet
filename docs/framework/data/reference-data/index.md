# Données référentielles

`Granit.ReferenceData` fournit un cadre générique pour gérer les données
référentielles (pays, devises, langues, types de documents, etc.) avec
audit HDS, filtrage automatique des entrées inactives et cache mémoire.

## Architecture

Le module est composé de 3 packages NuGet :

| Package | Rôle |
| --- | --- |
| `Granit.ReferenceData` | Entité de base, interfaces store/seeder, options |
| `Granit.ReferenceData.EntityFrameworkCore` | Store EF Core avec cache, configuration, bridge seeding |
| `Granit.ReferenceData.Endpoints` | Endpoints Minimal API (CRUD) |

## Installation

```bash
dotnet add package Granit.ReferenceData
dotnet add package Granit.ReferenceData.EntityFrameworkCore
dotnet add package Granit.ReferenceData.Endpoints
```

## Entité de base

Toute donnée référentielle hérite de `ReferenceDataEntity` :

```csharp
public sealed class Country : ReferenceDataEntity
{
    public string Alpha3Code { get; set; } = string.Empty;
    public string CallingCode { get; set; } = string.Empty;
}
```

`ReferenceDataEntity` hérite de `AuditedEntity` (audit HDS 3 ans) et implémente
`IActive` (filtre global EF Core automatique). Propriétés fournies :

- `Guid Id` — identifiant unique (hérité de `Entity`)
- `string Code` — clé métier unique (ex : "BE", "EUR")
- `string Label` — libellé par défaut (anglais)
- `bool IsActive` — activation/désactivation logique
- `int SortOrder` — ordre d'affichage
- `DateTimeOffset? ValidFrom` / `ValidTo` — période de validité optionnelle

## Configuration

### Avec le système de modules

```csharp
builder.AddGranit<GranitReferenceDataModule>();
```

### Enregistrement direct

```csharp
services.AddGranitReferenceData();
```

### Options

La configuration est lue depuis la section `ReferenceData` :

```json
{
  "ReferenceData": {
    "CacheTimeToLive": "01:00:00"
  }
}
```

| Option | Par défaut | Description |
| --- | --- | --- |
| `CacheTimeToLive` | 1 heure | Durée de mise en cache des entrées |

## Store

L'interface `IReferenceDataStore<TEntity>` fournit les opérations CRUD :

```csharp
public interface IReferenceDataStore<TEntity>
{
    Task<ReferenceDataResult<TEntity>> GetAllAsync(ReferenceDataQuery? query, CancellationToken ct);
    Task<TEntity?> GetByCodeAsync(string code, CancellationToken ct);
    Task CreateAsync(TEntity entity, CancellationToken ct);
    Task UpdateAsync(TEntity entity, CancellationToken ct);
    Task SetActiveAsync(string code, bool isActive, CancellationToken ct);
}
```

### Requêtes

`ReferenceDataQuery` supporte le filtrage, le tri et la pagination :

```csharp
ReferenceDataResult<Country> result = await store.GetAllAsync(
    new ReferenceDataQuery(
        ActiveOnly: true,
        SearchTerm: "belg",
        SortBy: "Code",
        Skip: 0,
        Take: 25));
```

## Seeding

Implémenter `IReferenceDataSeeder<TEntity>` pour fournir les données initiales :

```csharp
public sealed class CountrySeeder : IReferenceDataSeeder<Country>
{
    public int Order => 1;

    public async Task SeedAsync(IReferenceDataStore<Country> store, CancellationToken ct)
    {
        // Upsert par Code (idempotent)
        Country? existing = await store.GetByCodeAsync("BE", ct);
        if (existing is null)
        {
            await store.CreateAsync(new Country
            {
                Code = "BE",
                Label = "Belgium",
                Alpha3Code = "BEL",
            }, ct);
        }
    }
}
```

Enregistrer le seeder dans le conteneur DI :

```csharp
services.AddTransient<IReferenceDataSeeder<Country>, CountrySeeder>();
```

Le `ReferenceDataSeedContributor` (bridge) est enregistré automatiquement
par `AddReferenceDataStore<TEntity, TDbContext>()` et intègre les seeders
dans le pipeline `IDataSeedContributor` existant.

## Voir aussi

- [EF Core](efcore.md) — configuration de la persistance
- [Endpoints](endpoints.md) — endpoints Minimal API
