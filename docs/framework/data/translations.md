# Entités traduisibles

## Vue d'ensemble

Granit fournit un mécanisme pour stocker des **données métier multilingues**
en base de données. Ce besoin est distinct de la localisation d'interface
(fichiers `.resx` / `Granit.Localization`) — il s'agit ici de données
utilisateur persistées (titre de document, nom de catégorie, sujet d'email).

Deux cas d'usage coexistent sur le **même modèle de données** :

| Cas | Exemple | Comportement |
| --- | --- | --- |
| Propriétés traduisibles | Titre de document | Fallback vers une autre culture |
| Entités culture-variantes | Template d'email | `null` si la culture n'existe pas |

Le framework fournit le **mécanisme** (stockage + résolution).
L'application décide de la **politique** (fallback ou strict).

## Déclarer une entité traduisible

### Entité parente

L'entité parente implémente `ITranslatable<TTranslation>` :

```csharp
public sealed class Document : AuditedEntity, ITranslatable<DocumentTranslation>
{
    public string InternalCode { get; set; } = string.Empty;
    public ICollection<DocumentTranslation> Translations { get; set; } = [];
}
```

### Classe de traduction

Deux classes de base sont disponibles :

- **`Translation<TParent>`** — minimale (hérite de `Entity`, pas d'audit)
- **`AuditedTranslation<TParent>`** — avec audit HDS (hérite de `AuditedEntity`,
  champs `CreatedAt/By`, `ModifiedAt/By` remplis automatiquement)

```csharp
// Sans audit
public sealed class CategoryTranslation : Translation<Category>
{
    public string Name { get; set; } = string.Empty;
}

// Avec audit HDS (recommandé pour les données de santé)
public sealed class DocumentTranslation : AuditedTranslation<Document>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

Les deux classes fournissent les propriétés `ParentId` (FK) et `Culture`
(BCP 47, ex. `"fr"`, `"en-US"`, `"nl-BE"`).

## Configuration EF Core

### Automatique via conventions

`ApplyGranitConventions()` détecte automatiquement les entités implémentant
`ITranslation<TParent>` et configure :

- **FK** `ParentId → Parent.Id` avec **cascade delete**
- **Index unique** sur `(ParentId, Culture)` — une seule traduction par culture
- **Max length** de `Culture` à 20 caractères

Aucune configuration manuelle n'est nécessaire :

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.ApplyGranitConventions(currentTenant, dataFilter);
```

### Configuration personnalisée

Si nécessaire, le consommateur peut affiner la configuration via
`IEntityTypeConfiguration<T>` (ex. table name, max length sur les propriétés) :

```csharp
internal sealed class DocumentTranslationConfiguration
    : IEntityTypeConfiguration<DocumentTranslation>
{
    public void Configure(EntityTypeBuilder<DocumentTranslation> builder)
    {
        builder.ToTable("document_translations");
        builder.Property(t => t.Title).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(4000);
    }
}
```

## Résolution

La méthode d'extension `GetTranslation` opère sur les traductions **déjà
chargées en mémoire** (après `Include`) :

```csharp
// Cas 1 — Fallback (propriété traduisible)
DocumentTranslation? translation = document.GetTranslation("fr-BE");
// Résolution : fr-BE → fr → en (défaut) → première disponible

// Cas 2 — Strict (entité culture-variante)
EmailTemplateTranslation? translation = template.GetTranslation("nl-BE", useFallback: false);
// Résolution : nl-BE exact ou null
```

### Chaîne de résolution (fallback)

1. Match exact de culture (`fr-BE`)
2. Remontée hiérarchique (`fr-BE` → `fr`)
3. Fallback vers la culture par défaut (`en`, configurable)
4. Première traduction disponible

### Culture par défaut

La culture de fallback est configurable par appel :

```csharp
document.GetTranslation("de", defaultCulture: "fr");
```

## Extensions LINQ

### Inclusion

```csharp
// Toutes les traductions
query.IncludeTranslations<Document, DocumentTranslation>()

// Une seule culture
query.IncludeTranslations<Document, DocumentTranslation>("fr")
```

### Filtrage

```csharp
// Documents dont le titre FR contient "rapport"
query.WhereTranslation<Document, DocumentTranslation>(
    "fr", t => t.Title.Contains("rapport"))
// SQL : WHERE EXISTS (SELECT 1 FROM ... WHERE Culture = 'fr' AND Title LIKE '%rapport%')
```

### Tri

```csharp
// Tri alphabétique par titre FR
query.OrderByTranslation<Document, DocumentTranslation, string>(
    "fr", t => t.Title)
```

### Alternative : LINQ natif

Le développeur peut toujours écrire le LINQ EF Core directement :

```csharp
query.Where(e => e.Translations.Any(
    t => t.Culture == "fr" && t.Title.Contains("rapport")))
```

## Interaction avec le soft delete

Quand l'entité parente implémente `ISoftDeletable`, la suppression logique
(`IsDeleted = true`) **ne déclenche pas** le cascade delete. Les traductions
restent liées au parent et sont exclues des requêtes par le query filter du
parent. C'est le comportement correct pour la conformité HDS (conservation
3 ans).

## Approches alternatives écartées

| Approche | Raison de l'exclusion |
| --- | --- |
| Colonnes par langue (`TitleFr`, `TitleEn`) | Ne scale pas ; migration DB à chaque nouvelle langue |
| JSON / `jsonb` (`TranslatedString`) | Recherche et tri coûteux sur SQL Server ; pas d'indexation native |
| Table globale (`DataTranslation`) | Pas de FK ; orphelins ; complexité opérationnelle ; perte de typage |
