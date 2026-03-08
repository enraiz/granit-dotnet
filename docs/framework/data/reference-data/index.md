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
- `string LabelEn` — libellé anglais (fallback, toujours persisté)
- `string LabelFr` — libellé français
- `string LabelNl` — libellé néerlandais
- `string LabelDe` — libellé allemand
- `string LabelEs` — libellé espagnol
- `string LabelIt` — libellé italien
- `string LabelPt` — libellé portugais
- `string Label` — propriété virtuelle `[NotMapped]`, résout automatiquement le
  libellé selon `CultureInfo.CurrentUICulture` parmi les 7 langues supportées ;
  retombe sur `LabelEn` si la traduction est vide ou la culture non supportée.
  Surchargeable pour une logique de résolution personnalisée
- `bool IsActive` — activation/désactivation logique
- `int SortOrder` — ordre d'affichage
- `DateTimeOffset? ValidFrom` / `ValidTo` — période de validité optionnelle

> **Note** : les libellés de traduction sont optionnels. Une application n'utilisant
> que 2 langues (ex : fr et en) peut laisser les autres libellés vides — la propriété
> `Label` retombera automatiquement sur `LabelEn`.

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

Les interfaces `IReferenceDataStoreReader<TEntity>` et `IReferenceDataStoreWriter<TEntity>`
séparent les opérations de lecture et d'écriture (CQRS) :

```csharp
public interface IReferenceDataStoreReader<TEntity>
{
    Task<ReferenceDataResult<TEntity>> GetAllAsync(ReferenceDataQuery? query, CancellationToken ct);
    Task<TEntity?> GetByCodeAsync(string code, CancellationToken ct);
}

public interface IReferenceDataStoreWriter<TEntity>
{
    Task CreateAsync(TEntity entity, CancellationToken ct);
    Task UpdateAsync(TEntity entity, CancellationToken ct);
    Task SetActiveAsync(string code, bool isActive, CancellationToken ct);
}
```

### Requêtes

`ReferenceDataQuery` supporte le filtrage, le tri et la pagination :

```csharp
ReferenceDataResult<Country> result = await storeReader.GetAllAsync(
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

    public async Task SeedAsync(
        IReferenceDataStoreReader<Country> storeReader,
        IReferenceDataStoreWriter<Country> storeWriter,
        CancellationToken ct)
    {
        // Upsert par Code (idempotent)
        Country? existing = await storeReader.GetByCodeAsync("BE", ct);
        if (existing is null)
        {
            await storeWriter.CreateAsync(new Country
            {
                Code = "BE",
                LabelEn = "Belgium",
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
