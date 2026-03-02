# Data Import

`Granit.DataImport` est le socle du pipeline d'import de données du framework Granit.
Il fournit un mini-ETL intégré : **Extract → Map → Validate → Execute**, avec un moteur
de suggestion de mapping intelligent à 4 niveaux et un support de roundtrip
(INSERT vs UPDATE).

```text
Granit.Core + Granit.Timing + Granit.Validation
                    │
            Granit.DataImport           ← socle (interfaces + pipeline)
            ┌───────┼────────┐
            │       │        │
     .Csv (Sep)  .Excel   .EntityFrameworkCore
                (Sylvan)    (EF executor + stores)
                                  │
                           .Endpoints
                          (REST API, Wolverine)
```

> **Packages de parsing** : `Granit.DataImport.Csv` utilise
> [Sep](https://github.com/nietras/Sep) (MIT, SIMD) et `Granit.DataImport.Excel`
> utilise [Sylvan.Data.Excel](https://github.com/MarkPflug/Sylvan.Data.Excel)
> (MIT, zero-dep). ClosedXML reste dédié à la **génération** dans
> `Granit.DocumentGeneration.Excel`.

## Installation

```bash
dotnet add package Granit.DataImport
```

Puis ajouter un ou plusieurs parseurs :

```bash
dotnet add package Granit.DataImport.Csv
dotnet add package Granit.DataImport.Excel
```

## Enregistrement DI

```csharp
// Socle (interfaces, pipeline, options)
services.AddGranitDataImport();

// Parseurs (au moins un requis)
services.AddGranitDataImportCsv();
services.AddGranitDataImportExcel();

// Définition d'import par entité
services.AddImportDefinition<Patient, PatientImportDefinition>();

// IA optionnelle (remplace le NullSemanticMappingService par défaut)
services.AddSemanticMappingService<MistralSemanticMappingService>();
```

## Pipeline

Le pipeline complet est exécuté en arrière-plan (Wolverine) :

```text
Upload (fichier) → Preview (headers + suggestions) → Confirm mappings → Execute
```

### Étapes d'exécution

```text
IFileParser.ParseAsync()
  → IAsyncEnumerable<RawImportRow>        1 ligne à la fois (streaming)
          │
          ▼  (optionnel, si GroupBy déclaré)
IRowGrouper.GroupAsync()
  → IAsyncEnumerable<GroupedRows>         1 groupe = 1 entité agrégée
          │
          ▼
IDataMapper<TEntity>.MapAsync()
  → MappingResult<TEntity>                conversion string → CLR
          │
          ▼  (si conversion OK)
IRowValidator<TEntity>.ValidateAsync()
  → RowValidationResult                   FluentValidation
          │
          ▼  (si valide)
IRecordIdentityResolver<TEntity>.ResolveAsync()
  → RecordIdentity (Insert | Update)      roundtrip
          │
          ▼  (par batch, 500 par défaut)
IImportExecutor<TEntity>.ExecuteAsync()
  → ImportReport                          stats + erreurs uniquement
```

Tout le pipeline est en streaming via `IAsyncEnumerable`. Seul le batch courant
(500 entités) et les erreurs cumulées sont en mémoire.

## Suggestion de mapping (4 niveaux)

`IMappingSuggestionService` applique 4 stratégies par ordre de confiance décroissante.
Les colonnes déjà matchées par un niveau supérieur sont exclues des niveaux suivants.

| Niveau | Source | Confiance |
| --- | --- | --- |
| 1. Sauvegardé | `IMappingStore` (mappings précédents) | `Saved` |
| 2. Exact | Nom de propriété, DisplayName ou alias (case-insensitive) | `Exact` |
| 3. Fuzzy | Distance de Levenshtein normalisée (seuil configurable, défaut 0.8) | `Fuzzy` |
| 4. Sémantique | `ISemanticMappingService` (IA optionnelle) | `Semantic` |

> **Garantie RGPD/HDS** : l'interface `ISemanticMappingService` ne reçoit que les
> **en-têtes** de colonnes et les **métadonnées de schéma** (`FieldMetadata`).
> Aucune donnée métier (`RawImportRow.Values`) ne traverse la frontière IA.

## Import Definition (Fluent API)

Chaque entité importable est déclarée via une `ImportDefinition<TEntity>` :

```csharp
public sealed class PatientImportDefinition : ImportDefinition<Patient>
{
    public override string Name => "Guava.PatientImport";

    protected override void Configure(ImportDefinitionBuilder<Patient> builder)
    {
        builder
            .HasBusinessKey(p => p.Niss)
            .Property(p => p.Niss, p => p
                .DisplayName("NISS")
                .Aliases("Numéro national", "National ID")
                .Required())
            .Property(p => p.FirstName, p => p
                .DisplayName("Prénom")
                .Aliases("First Name", "Voornaam"))
            .Property(p => p.LastName, p => p
                .DisplayName("Nom")
                .Aliases("Last Name", "Achternaam"))
            .Property(p => p.Email, p => p
                .DisplayName("Email")
                .Aliases("Courriel", "Mail", "E-mail"))
            .Property(p => p.BirthDate, p => p
                .DisplayName("Date de naissance")
                .Format("dd/MM/yyyy"))
            .ExcludeOnUpdate(p => p.CreatedAt);
    }
}
```

### Propriétés de la Fluent API

| Méthode | Description |
| --- | --- |
| `Property(expr, config?)` | Déclare une propriété importable (whitelist explicite) |
| `HasBusinessKey(expr)` | Clé métier pour résolution INSERT vs UPDATE |
| `HasCompositeKey(exprs)` | Clé composite (plusieurs propriétés) |
| `HasExternalId()` | Active la résolution par External ID (pattern Odoo) |
| `ExcludeOnUpdate(expr)` | Propriété jamais écrasée lors d'un UPDATE |
| `GroupBy(column)` | Regroupement parent/enfants par colonne |
| `HasMany(collection, config)` | Collection enfant (requiert `GroupBy`) |

### Import parent/enfants (GroupBy)

Pour un fichier plat où un parent s'étend sur plusieurs lignes :

```csharp
public sealed class OrderImportDefinition : ImportDefinition<Order>
{
    public override string Name => "Guava.OrderImport";

    protected override void Configure(ImportDefinitionBuilder<Order> builder)
    {
        builder
            .GroupBy("Numéro")
            .Property(o => o.OrderNumber, p => p.DisplayName("Numéro").Required())
            .Property(o => o.ClientName, p => p.DisplayName("Client"))
            .HasMany(o => o.Lines, child =>
            {
                child.Property(l => l.ProductName, p => p.DisplayName("Produit"));
                child.Property(l => l.Quantity, p => p.DisplayName("Quantité"));
            })
            .HasBusinessKey(o => o.OrderNumber);
    }
}
```

Le fichier doit être trié par clé de groupe. Seul le groupe courant est bufferisé
en mémoire.

## Configuration

```json
{
  "DataImport": {
    "DefaultMaxFileSizeMb": 50,
    "DefaultBatchSize": 500,
    "FuzzyMatchThreshold": 0.8
  }
}
```

| Option | Défaut | Description |
| --- | --- | --- |
| `DefaultMaxFileSizeMb` | 50 | Taille maximale de fichier (Mo) |
| `DefaultBatchSize` | 500 | Taille des batchs pour l'exécution |
| `FuzzyMatchThreshold` | 0.8 | Seuil de similarité Levenshtein (0.0–1.0) |

## Rapport d'import

`ImportReport` ne contient que les statistiques globales et les lignes en erreur
(pas les lignes réussies). Pour 100 000 lignes et 50 erreurs, seuls ~50 objets
`ImportRowError` sont en mémoire.

```csharp
ImportReport report = await executor.ExecuteAsync(entities, options);

// Statistiques
report.TotalRows;       // 100000
report.SucceededRows;   // 99950
report.FailedRows;      // 50
report.InsertedRows;    // 80000
report.UpdatedRows;     // 19950
report.Duration;        // TimeSpan

// Erreurs détaillées (seules les lignes en erreur)
foreach (ImportRowError error in report.RowErrors)
{
    // error.RowNumber, error.Kind, error.ErrorCodes, error.Message
}
```

## Packages associés

| Package | Rôle |
| --- | --- |
| `Granit.DataImport.Csv` | Parseur CSV via Sep (SIMD, zero-alloc) |
| `Granit.DataImport.Excel` | Parseur Excel via Sylvan.Data.Excel (streaming) |
| `Granit.DataImport.EntityFrameworkCore` | Executor EF Core, stores, identity resolvers |
| `Granit.DataImport.Endpoints` | API REST + Wolverine (upload, preview, execute, rapport) |

## Voir aussi

- ADR-019 — Sep pour le parsing CSV
- ADR-020 — Sylvan.Data.Excel pour le parsing Excel
