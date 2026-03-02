# Données référentielles — EF Core

`Granit.ReferenceData.EntityFrameworkCore` fournit l'implémentation EF Core
du store de données référentielles avec cache mémoire intégré.

## Installation

```bash
dotnet add package Granit.ReferenceData.EntityFrameworkCore
```

## Configuration du DbContext

### 1. Configuration de l'entité

Créer une configuration héritant de `ReferenceDataEntityTypeConfiguration<T>` :

```csharp
public sealed class CountryConfiguration
    : ReferenceDataEntityTypeConfiguration<Country>
{
    public CountryConfiguration() : base("ref_countries") { }

    protected override void ConfigureEntity(EntityTypeBuilder<Country> builder)
    {
        builder.Property(e => e.Alpha3Code)
               .HasMaxLength(3)
               .IsRequired();

        builder.Property(e => e.CallingCode)
               .HasMaxLength(10);
    }
}
```

La classe de base configure automatiquement :

- `HasKey(e => e.Id)`
- Index unique sur `Code` (`uq_{table}_code`)
- Index sur `IsActive` (`ix_{table}_is_active`)
- `Label` est ignoré (propriété virtuelle `[NotMapped]`)
- Colonnes `Code` (50 car.), `LabelEn` (250 car., obligatoire), `SortOrder` (défaut 0)
- Colonnes de traduction : `LabelFr`, `LabelNl`, `LabelDe`, `LabelEs`, `LabelIt`,
  `LabelPt` (250 car. chacune, optionnelles)
- Colonnes d'audit HDS (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`)

> **Migration** : l'ajout des 6 colonnes de traduction nécessite une migration
> EF Core pour chaque table référentielle existante. Les colonnes acceptent les
> chaînes vides par défaut — aucune donnée existante n'est impactée.

### 2. Appliquer la configuration dans OnModelCreating

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ConfigureReferenceData(new CountryConfiguration());
    modelBuilder.ConfigureReferenceData(new CurrencyConfiguration());
}
```

### 3. Enregistrer le store

```csharp
services.AddReferenceDataStore<Country, AppDbContext>();
services.AddReferenceDataStore<Currency, AppDbContext>();
```

Cela enregistre :

- `IReferenceDataStore<T>` (Scoped) — le store EF Core avec cache mémoire
- `IDataSeedContributor` — le bridge pour les seeders typés

## Pattern Shared DbContext

Le store utilise le DbContext de l'application (pas de DbContext dédié).
Le mécanisme `IServiceScopeFactory` crée un scope dédié par opération,
garantissant la sécurité des accès concurrents.

## Cache mémoire

Le store met en cache les résultats de `GetByCodeAsync` avec un TTL
configurable via `ReferenceDataOptions.CacheTimeToLive` (défaut : 1 heure).

Le cache est invalidé automatiquement lors des opérations d'écriture
(`CreateAsync`, `UpdateAsync`, `SetActiveAsync`).

## Seeding

Les seeders typés (`IReferenceDataSeeder<T>`) sont exécutés via le
`ReferenceDataSeedContributor` qui s'intègre dans le pipeline
`IDataSeedContributor` de `Granit.Persistence`.

L'exécution est :

- **Ordonnée** — par la propriété `Order` (ascendant)
- **Résiliente** — les erreurs sont loguées mais n'empêchent pas les
  seeders suivants de s'exécuter

## Voir aussi

- [Socle](index.md) — entités, interfaces et options
- [Endpoints](endpoints.md) — endpoints Minimal API
