# Données référentielles — Endpoints

`Granit.ReferenceData.Endpoints` fournit des endpoints Minimal API
pour consulter et administrer les données référentielles.

## Installation

```bash
dotnet add package Granit.ReferenceData.Endpoints
```

## Enregistrement des endpoints

```csharp
app.MapReferenceDataEndpoints<Country>();
app.MapReferenceDataEndpoints<Currency>(opts =>
{
    opts.RoutePrefix = "api/ref";
    opts.AdminPolicyName = "Custom.Admin";
});
```

Le nom de l'entité est converti en kebab-case pour le segment de route.
Exemple : `Country` → `/reference-data/country`.

## Endpoints disponibles

### Lecture (publique)

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/{prefix}/{entity}` | Liste filtrée et paginée |
| `GET` | `/{prefix}/{entity}/{code}` | Détail par code |

#### Paramètres de requête (GET liste)

| Paramètre | Type | Par défaut | Description |
| --- | --- | --- | --- |
| `activeOnly` | `bool` | `true` | Filtrer les entrées actives uniquement |
| `search` | `string?` | `null` | Recherche dans Code et tous les libellés (en, fr, nl, de, es, it, pt) |
| `sortBy` | `string?` | `SortOrder` | Propriété de tri (`Code`, `Label`, `SortOrder`) |
| `descending` | `bool` | `false` | Tri descendant |
| `skip` | `int?` | `null` | Nombre d'entrées à ignorer |
| `take` | `int?` | `null` | Nombre d'entrées à retourner |

### Administration (protégée)

| Méthode | Route | Code HTTP | Description |
| --- | --- | --- | --- |
| `POST` | `/{prefix}/{entity}` | 201 Created | Créer une entrée |
| `PUT` | `/{prefix}/{entity}/{code}` | 200 OK | Modifier une entrée |
| `DELETE` | `/{prefix}/{entity}/{code}` | 204 No Content | Désactiver une entrée |

Les endpoints d'administration sont protégés par une politique d'autorisation
configurable (par défaut : `ReferenceData.Admin`, rôle `granit-reference-data-admin`).

## Options

| Option | Par défaut | Description |
| --- | --- | --- |
| `RoutePrefix` | `reference-data` | Préfixe de route |
| `TagName` | `Reference Data` | Tag OpenAPI pour Swagger |
| `AdminPolicyName` | `ReferenceData.Admin` | Politique d'autorisation admin |
| `RequiredRole` | `granit-reference-data-admin` | Rôle requis (fallback) |

## DTOs

### ReferenceDataCreateRequest

```csharp
record ReferenceDataCreateRequest(
    string Code,
    string LabelEn,
    string LabelFr = "",
    string LabelNl = "",
    string LabelDe = "",
    string LabelEs = "",
    string LabelIt = "",
    string LabelPt = "",
    int SortOrder = 0,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null);
```

### ReferenceDataUpdateRequest

```csharp
record ReferenceDataUpdateRequest(
    string LabelEn,
    string LabelFr = "",
    string LabelNl = "",
    string LabelDe = "",
    string LabelEs = "",
    string LabelIt = "",
    string LabelPt = "",
    int SortOrder = 0,
    bool IsActive = true,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null);
```

Les libellés de traduction sont optionnels (valeur par défaut : chaîne vide).
Seul `LabelEn` est obligatoire.

## Voir aussi

- [Socle](index.md) — entités, interfaces et options
- [EF Core](efcore.md) — configuration de la persistance
