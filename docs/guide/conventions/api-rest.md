# Conventions REST API

[← Index des conventions](index.md)

Ce document centralise les conventions REST pour les APIs Granit. Il couvre le
nommage, les formats, la pagination, la gestion des erreurs et les bonnes pratiques
de conception. Les sujets spécifiques (OpenAPI, versioning, codes HTTP) sont
documentés séparément et référencés ci-dessous.

> **Voir aussi** :
>
> - [Codes de retour HTTP](../../framework/api/http-responses.md) — sémantique 2xx / 4xx / 5xx
> - [API Versioning](../../framework/api/api-versioning.md) — stratégie URL, dépréciation
> - [API Documentation](../../framework/api/api-documentation.md) — OpenAPI, Scalar, transformers
> - [Idempotency](../../framework/api/idempotency.md) — clé d'idempotence, cache Redis
> - [Architecture](architecture.md) — structure des endpoints, patterns Minimal API
> - [Style et nommage](style-et-nommage.md) — DTOs, suffixes Request/Response

## Nommage

### Propriétés JSON : camelCase

Toutes les propriétés JSON utilisent **camelCase**. C'est le défaut de
`System.Text.Json` en .NET — aucune configuration supplémentaire n'est nécessaire.

```json
{
  "firstName": "Alice",
  "lastName": "Dupont",
  "createdAt": "2025-03-15T10:30:00Z"
}
```

> **Pourquoi pas snake_case ?** Les Zalando RESTful API Guidelines recommandent
> `snake_case`, mais l'écosystème .NET et les générateurs de clients (orval,
> openapi-typescript, Kiota) fonctionnent nativement en camelCase. Changer
> introduirait des frictions sans bénéfice mesurable.

### Enums : PascalCase (valeurs string)

Les enums sont sérialisés en **PascalCase string**, pas en entiers ni en
UPPER_SNAKE_CASE.

```json
{ "status": "InProgress" }
```

Configuration globale (déjà appliquée par `Granit.Core`) :

```csharp
options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
```

> **Pourquoi pas UPPER_SNAKE_CASE ?** PascalCase est la convention C# native.
> Les clients TypeScript générés par orval/openapi-typescript conservent le même
> casing, évitant toute transformation.

### URLs : kebab-case

Les segments d'URL utilisent **kebab-case** pour les noms de ressources :

```text
GET  /api/v1/background-jobs
POST /api/v1/reference-data
GET  /api/v1/saved-views/{id}
```

Les paramètres de route restent en camelCase : `{patientId}`, `{tenantId}`.

## Préfixe `/api`

Le préfixe `/api` est une **décision au niveau de l'application**, pas du
framework. Granit ne l'impose pas.

**Quand utiliser `/api`** : applications exposant à la fois une UI et une API
sur le même domaine (ex. : `app.example.com/api/v1/patients` pour l'API,
`app.example.com/` pour le frontend).

**Quand l'omettre** : services purement API (microservices, services inter-services)
où chaque service a son propre domaine ou sous-domaine.

```csharp
// Application avec UI — préfixe /api
var api = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

// Service pur API — pas de préfixe
var api = app.MapGroup("v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);
```

## Formats de données

### Dates et heures : ISO 8601

Toutes les dates et heures sont au format **ISO 8601** avec fuseau horaire.
C'est le comportement par défaut de `System.Text.Json`.

| Type | Format | Exemple |
| ---- | ------ | ------- |
| Date + heure UTC | `yyyy-MM-ddTHH:mm:ssZ` | `2025-03-15T10:30:00Z` |
| Date + heure avec offset | `yyyy-MM-ddTHH:mm:ss±HH:mm` | `2025-03-15T11:30:00+01:00` |
| Date seule | `yyyy-MM-dd` | `2025-03-15` |
| Durée | ISO 8601 duration | `P1DT2H30M` |

> **Rappel** : ne jamais utiliser `DateTime.Now` / `DateTime.UtcNow`.
> Injecter `TimeProvider` ou `IClock` (voir [implementation.md](implementation.md)).

### Identifiants : UUID v7

Les identifiants sont des **UUID v7** (séquentiels, ordonnables par temps).
Sérialisés en format standard avec tirets : `"0193a5b2-7c3d-7def-8a12-bc3456789abc"`.

## Pagination

Granit supporte deux modes de pagination, configurables par `QueryDefinition<T>`.

### Pagination offset (défaut)

Le mode par défaut. Adapté aux listes avec navigation par page et compteur total.

```text
GET /api/v1/patients?page=1&pageSize=20&sort=-createdAt
```

Réponse :

```json
{
  "items": [...],
  "totalCount": 142,
  "hasMore": true
}
```

| Paramètre | Type | Défaut | Description |
| --------- | ---- | ------ | ----------- |
| `page` | `int` | `1` | Numéro de page (1-based) |
| `pageSize` | `int` | `20` | Taille de page (plafonné à `MaxPageSize`, défaut 100) |
| `sort` | `string` | Défini par `QueryDefinition` | Champs séparés par virgule, préfixe `-` pour tri descendant |

#### Opt-in SkipTotalCount

Sur les tables volumineuses, le `COUNT(*)` peut être coûteux. L'appelant peut
demander à ne pas calculer le total :

```text
GET /api/v1/patients?page=1&pageSize=20&skipTotalCount=true
```

```json
{
  "items": [...],
  "totalCount": null,
  "hasMore": true
}
```

Quand `skipTotalCount=true` :

- `totalCount` est `null`
- `hasMore` est calculé en récupérant `pageSize + 1` lignes (la ligne
  supplémentaire n'est pas retournée)
- Le client ne peut pas afficher « page 3 sur 8 » mais peut afficher « Suivant / Précédent »

### Pagination cursor (keyset)

Mode opt-in pour les flux en temps réel, les listes infinies et les jeux de
données volumineux. Activé par `SupportsCursorPagination()` dans la
`QueryDefinition<T>`.

```text
GET /api/v1/events?pageSize=50
GET /api/v1/events?cursor=eyJpZCI6MTIzfQ&pageSize=50
```

Réponse :

```json
{
  "items": [...],
  "totalCount": null,
  "nextCursor": "eyJpZCI6MTczfQ",
  "hasMore": true
}
```

| Paramètre | Type | Description |
| --------- | ---- | ----------- |
| `cursor` | `string` | Curseur opaque (Base64). Absent pour la première page |
| `pageSize` | `int` | Taille de page |

Le curseur est **opaque** — le client ne doit jamais le décoder ni le construire.
Le serveur encode la valeur de la propriété de tri (typiquement l'ID) en Base64
JSON.

**Quand utiliser cursor vs offset** :

| Critère | Offset | Cursor |
| ------- | ------ | ------ |
| Navigation par numéro de page | Oui | Non |
| Compteur total | Oui (opt-out possible) | Non |
| Données qui changent fréquemment | Résultats peuvent sauter/doubler | Stable |
| Performances sur gros volumes | `OFFSET N` ralentit avec N | Constant (`WHERE id > X`) |
| Scroll infini / flux temps réel | Non recommandé | Recommandé |

### Proposer les deux modes

Un même endpoint peut supporter les deux modes si la `QueryDefinition` déclare
`SupportsCursorPagination()`. Le mode est déterminé par la présence du
paramètre `cursor` :

- `?page=2&pageSize=20` → offset
- `?cursor=abc&pageSize=20` → cursor

Le endpoint `/meta` expose `pagination.supportsCursor: true` pour que le
frontend choisisse le mode approprié.

## Tri

Le tri utilise une syntaxe à champs séparés par virgule. Le préfixe `-` indique
un tri descendant :

```text
GET /api/v1/patients?sort=-createdAt,lastName
```

Seuls les champs déclarés `Sortable()` dans la `QueryDefinition` sont acceptés.
Un champ non sortable retourne un **400 Bad Request**.

## Filtres

### Filtres par champ

Syntaxe : `filter[field.operator]=value`

```text
GET /api/v1/patients?filter[name.contains]=Alice&filter[age.gte]=18
```

Opérateurs supportés : `eq`, `contains`, `startsWith`, `endsWith`, `gt`, `gte`,
`lt`, `lte`, `in`, `between`.

### Presets (filtres prédéfinis)

```text
GET /api/v1/patients?presets[status]=active,pending
```

### Quick filters (toggles)

```text
GET /api/v1/patients?quickFilters=MyPatients,Unread
```

### Recherche globale

```text
GET /api/v1/patients?search=Alice
```

Recherche sur les champs déclarés via `GlobalSearch()` dans la `QueryDefinition`.

## Réponses d'erreur : RFC 7807

Toutes les erreurs (4xx et 5xx) utilisent le format `application/problem+json`
(RFC 7807). Utiliser **toujours** `TypedResults.Problem()`, jamais
`TypedResults.BadRequest<string>()`.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation failed",
  "status": 422,
  "detail": "The NISS format is invalid.",
  "instance": "/api/v1/patients"
}
```

> **Convention** : le type de retour des endpoints doit utiliser les union types
> `Results<Ok<T>, ProblemHttpResult>` pour que la documentation OpenAPI génère
> automatiquement les schémas de réponse.
> Voir [architecture.md](architecture.md) pour les patterns complets.

## Opérations batch : HTTP 207 Multi-Status

Pour les opérations qui traitent plusieurs ressources et où certaines peuvent
échouer indépendamment, utiliser **207 Multi-Status** avec un `BatchResult<T>`.

```csharp
public sealed record BatchResult<T>(
    IReadOnlyList<BatchItemResult<T>> Results,
    int SuccessCount,
    int FailureCount);

public sealed record BatchItemResult<T>(
    T? Value,
    bool IsSuccess,
    ProblemDetails? Error);
```

```json
{
  "results": [
    { "value": { "id": "abc", "status": "Created" }, "isSuccess": true, "error": null },
    { "value": null, "isSuccess": false, "error": { "status": 422, "detail": "NISS invalide" } }
  ],
  "successCount": 1,
  "failureCount": 1
}
```

**Quand utiliser 207** :

- Import de données en batch
- Opérations bulk (suppression, mise à jour de statut)
- Envoi de notifications à plusieurs destinataires

**Quand NE PAS utiliser 207** :

- Opération unitaire → utiliser le code approprié (200, 201, 204, 4xx)
- Opération transactionnelle (tout ou rien) → utiliser 200 ou 4xx/5xx

## Headers de dépréciation

Quand un endpoint ou une version d'API est dépréciée, le serveur ajoute des
headers standards pour informer les consommateurs :

| Header | Format | Description |
| ------ | ------ | ----------- |
| `Deprecation` | `true` | Indique que l'endpoint est déprécié |
| `Sunset` | HTTP-date (RFC 7231) | Date après laquelle l'endpoint sera retiré |
| `Link` | `<url>; rel="deprecation"` | Lien vers la documentation de migration |

```http
HTTP/1.1 200 OK
Deprecation: true
Sunset: Sat, 01 Nov 2025 00:00:00 GMT
Link: <https://docs.example.com/migration/v1-to-v2>; rel="deprecation"
```

Voir [api-versioning.md](../../framework/api/api-versioning.md) pour la
configuration via l'attribut `[Deprecated]` et le middleware de dépréciation.

## Cacheabilité

Chaque type d'endpoint a une politique de cache recommandée :

| Type d'endpoint | Cache-Control | Justification |
| --------------- | ------------- | ------------- |
| Données utilisateur (`GET /me`, `/settings`) | `private, no-cache` | Données personnelles, vérification obligatoire |
| Données de référence (`GET /reference-data`) | `public, max-age=3600` | Changent rarement, partagées entre utilisateurs |
| Listes paginées (`GET /patients`) | `private, no-store` | Données sensibles (RGPD), changent fréquemment |
| Ressources statiques (images, documents) | `public, max-age=86400, immutable` | Content-addressable (hash dans l'URL) |
| Métadonnées de requête (`GET /meta`) | `public, max-age=3600` | Configuration stable |

### Helpers de cache

```csharp
// Données de référence — cache public 1h
app.MapGet("/reference-data", GetReferenceData)
    .CacheOutput(p => p.Expire(TimeSpan.FromHours(1)).Tag("reference-data"));

// Données utilisateur — pas de cache partagé
app.MapGet("/me", GetCurrentUser)
    .CacheOutput(p => p.NoCache().SetVaryByHeader("Authorization"));
```

> **RGPD** : les réponses contenant des données personnelles (PII) ne doivent
> **jamais** utiliser `public` dans `Cache-Control`. Utiliser `private` ou
> `no-store` pour éviter que des caches intermédiaires (CDN, reverse proxy)
> stockent des données sensibles.

## Considérations ISO 27001

- **Traçabilité** : les codes HTTP, headers de dépréciation et paramètres de
  pagination sont enregistrés dans les traces OpenTelemetry
- **Audit** : le versioning URL garantit que la version est visible dans les
  access logs (3 ans de rétention)
- **Chiffrement** : toutes les communications API utilisent TLS 1.2+
- **Données sensibles** : les réponses contenant des PII sont marquées
  `Cache-Control: private, no-store`
