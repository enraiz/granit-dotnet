# Spike : Intégration Wolverine HTTP + Microsoft.AspNetCore.OpenApi (.NET 10)

> **Issue** : [#275](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/275)
> **Date** : 2026-02-26
> **Statut** : Terminé — **GO**

## Objectif

Valider que les endpoints Wolverine HTTP sont correctement découverts et documentés
par le générateur OpenAPI natif de .NET 10 (`Microsoft.AspNetCore.OpenApi` 10.0.3),
avant de concevoir le module `Granit.OpenApi`.

## Stack testée

| Package | Version |
| ------- | ------- |
| .NET | 10.0 |
| `Microsoft.AspNetCore.OpenApi` | 10.0.3 |
| `Microsoft.OpenApi` | 2.0.0 (transitive) |
| `WolverineFx.Http` | 5.16.2 |
| `Scalar.AspNetCore` | 2.12.47 |
| `Microsoft.Extensions.ApiDescription.Server` | 10.0.3 |

## Résultats par question

### Q1 — Découverte des endpoints : OK

`MapWolverineEndpoints()` enregistre les routes dans le pipeline `IEndpointRouteBuilder`
standard d'ASP.NET Core. Le générateur `Microsoft.AspNetCore.OpenApi` les découvre
**automatiquement** via `IApiDescriptionProvider`.

**Preuve** : les 7 endpoints Wolverine apparaissent dans le document `v1.json` généré,
avec les bonnes routes, méthodes HTTP et tags.

```text
/internal/health/deep       → GET    [Internal]
/internal/diagnostics/config → GET    [Internal]
/api/patients               → GET    [Patients]
/api/patients               → POST   [Patients]
/api/patients/{id}          → GET    [Patients]
/api/patients/{id}          → DELETE [Patients]
/api/webhooks/sih           → POST   [Webhooks]
```

### Q2 — Propagation des métadonnées : OK

Toutes les métadonnées testées sont accessibles dans
`context.Description.ActionDescriptor.EndpointMetadata` à l'intérieur d'un
`IOpenApiOperationTransformer` :

| Métadonnée | Présente | Type dans EndpointMetadata |
| ---------- | -------- | -------------------------- |
| `[Authorize]` | Oui | `AuthorizeAttribute` |
| `[AllowAnonymous]` | Oui | `AllowAnonymousAttribute` |
| `[Tags("...")]` | Oui | `TagsAttribute` |
| `[AllowAnonymousTenant]` (custom) | Oui | `AllowAnonymousTenantAttribute` |
| `[WolverineGet]`, `[WolverinePost]`, etc. | Oui | Attributs Wolverine spécifiques |
| `HttpChain` (modèle interne Wolverine) | Oui | `Wolverine.Http.HttpChain` |
| `ProducesResponseTypeMetadata` | Oui | Automatique par Wolverine |
| `AcceptsMetadata` (body POST) | Oui | Automatique par Wolverine |

**Conséquence** : le transformer `X-Tenant-Id` peut détecter `[AllowAnonymousTenant]`
et le transformer JWT peut détecter `[Authorize]` / `[AllowAnonymous]` sans problème.

### Q3 — Schémas JSON des requêtes/réponses : OK avec nuances

#### Ce qui fonctionne parfaitement

- **Types concrets** en retour (records, classes) : schéma JSON complet avec
  propriétés, types, formats (`uuid`, `date`, `date-time`), nullable
- **Types de body** (commandes POST) : `requestBody` avec schéma `$ref` correct
- **Paramètres de route** (`{id}`) : détectés avec type `string` + format `uuid`
- **Tableaux** : `PatientListItem[]` → `{ "type": "array", "items": { "$ref": ... } }`
- **Nullable** : `string?` → `{ "type": ["null", "string"] }` (OpenAPI 3.1)
- **DELETE void** : correctement mappé en `204 No Content`
- **Components/schemas** : tous les types sont déplacés dans `#/components/schemas/`

#### Problèmes identifiés

1. **`IResult` opaque** : quand un handler retourne `IResult` (ex : `Results.Ok(...)`,
   `Results.Accepted()`), Wolverine déclare le type de réponse comme `IResult`.
   Le schéma généré est `{ "type": "object" }` — inutile pour la documentation.

   **Contournement** : utiliser des types de retour concrets ou `TypedResults` au
   lieu de `Results`, ou ajouter `[ProducesResponseType<ConcreteType>(200)]`
   explicitement.

2. **404 fantôme systématique** : Wolverine ajoute automatiquement une réponse `404
   Not Found` sur **tous** les endpoints, même ceux qui ne font jamais de lookup
   (ex : `GET /api/patients` qui retourne une liste). C'est du bruit documentaire.

   **Contournement** : un `IOpenApiOperationTransformer` peut supprimer les réponses
   `404` là où elles n'ont pas de sens (endpoints sans paramètre de route `{id}`).

### Q4 — Documents multiples : PARTIEL

Le mécanisme natif de documents multiples (`AddOpenApi("v1")` +
`AddOpenApi("v1-internal")`) fonctionne. Les deux documents sont générés.

**Problème** : sans `WithGroupName()` sur les endpoints Wolverine, les deux documents
contiennent **tous les endpoints**. Le document `v1-internal` est un miroir de `v1`.

Wolverine ne fournit pas de mécanisme natif pour appliquer `WithGroupName()` sur ses
endpoints. Le filtrage doit se faire via le delegate `ShouldInclude` en inspectant
les métadonnées :

```csharp
options.ShouldInclude = (description) =>
{
    IList<object> metadata = description.ActionDescriptor.EndpointMetadata;

    // Inclure les endpoints taggés "Internal"
    return metadata.OfType<TagsAttribute>()
        .Any(t => t.Tags.Contains("Internal"));
};
```

**Contournement validé** : `ShouldInclude` avec filtrage par `[Tags]` ou par un
attribut custom (`[InternalApi]`, `[PublicApi]`). Fonctionne car les métadonnées
sont correctement propagées (voir Q2).

### Q5 — Génération build-time : OK

`Microsoft.Extensions.ApiDescription.Server` 10.0.3 fonctionne avec Wolverine
**sans `ObjectDisposedException`** ni workaround.

Les deux documents sont générés dans le répertoire configuré :

```text
openapi/WolverineOpenApiSpike.json           (document "v1")
openapi/WolverineOpenApiSpike_v1-internal.json  (document "v1-internal")
```

La génération prend environ 3 secondes (Wolverine démarre, génère, s'arrête).

## Matrice de compatibilité

| Fonctionnalité | Statut | Workaround nécessaire |
| -------------- | ------ | --------------------- |
| Découverte des endpoints | OK | Non |
| Métadonnées `[Authorize]` | OK | Non |
| Métadonnées `[AllowAnonymous]` | OK | Non |
| Métadonnées `[Tags]` | OK | Non |
| Attributs custom | OK | Non |
| Schéma body (POST) | OK | Non |
| Schéma réponse (type concret) | OK | Non |
| Schéma réponse (`IResult`) | KO | Retourner des types concrets |
| Paramètres de route | OK | Non |
| Nullable | OK | Non |
| 404 automatique sur tous les endpoints | Bruit | Transformer pour filtrer |
| Documents multiples | Partiel | `ShouldInclude` custom |
| Génération build-time | OK | Non |
| Scalar UI | OK | Non |
| Security scheme (Bearer) | OK | `OpenApiSecuritySchemeReference` (API v2.0) |

## Points d'attention pour le module Granit.OpenApi

### Breaking changes Microsoft.OpenApi 2.0.0

.NET 10 utilise `Microsoft.OpenApi` 2.0.0 qui a des changements cassants majeurs :

- `Microsoft.OpenApi.Models` n'existe plus → tout est dans `Microsoft.OpenApi`
- `OpenApiSchema`, `OpenApiParameter`, `OpenApiResponse`, `OpenApiSecurityScheme`
  sont devenus des **interfaces** (`IOpenApiSchema`, `IOpenApiParameter`, etc.)
- Pour les références : utiliser `OpenApiSchemaReference`, `OpenApiParameterReference`,
  `OpenApiSecuritySchemeReference` (constructeur avec document)
- `OpenApiSecurityRequirement` attend `List<string>` (pas `string[]`)
- `OpenApiReference` n'existe plus en tant que propriété — utiliser les constructeurs
  des types `*Reference`

### Wolverine-specific

- `AddWolverineHttp()` est **obligatoire** (sinon `InvalidOperationException` au
  démarrage)
- Wolverine génère des `operationId` de style `GET_api_patients_id` — un transformer
  peut les rendre plus propres (`getPatientById`)
- Les `summary` et `description` sont identiques à l'`operationId` par défaut — il
  faudrait un transformer pour les enrichir à partir des XML docs ou d'un attribut

## Recommandation

### GO — Le module Granit.OpenApi est faisable

L'intégration Wolverine HTTP + `Microsoft.AspNetCore.OpenApi` fonctionne
correctement. Les 5 questions critiques ont des réponses positives (ou avec des
contournements simples via transformers).

Les workarounds identifiés sont tous résolubles au niveau du module `Granit.OpenApi`
(transformers globaux) sans impacter le code applicatif.

### Prérequis pour le design du module

1. **Convention de retour** : imposer des types concrets (pas `IResult`) dans les
   handlers Wolverine pour garantir des schémas exploitables
2. **Attribut `[InternalApi]`** : créer un attribut dédié plutôt que de réutiliser
   `[Tags]` pour le filtrage de documents
3. **Transformer de nettoyage** : supprimer les réponses 404 fantômes ajoutées par
   Wolverine sur les endpoints qui n'en ont pas besoin
4. **Sécurité HDS** : le document `v1-internal` et Scalar UI doivent être protégés
   par authentification ou désactivés en production
