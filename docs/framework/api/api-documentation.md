# API Documentation

`Granit.ApiDocumentation` génère les documents OpenAPI et expose
l'UI Scalar multi-version pour toutes les APIs Granit. Il dépend de
`Granit.ApiVersioning`.

> **Voir aussi** : [api-versioning.md](api-versioning.md) pour la configuration du
> versioning des routes.

## Choix technologiques

- **Génération OpenAPI** : `Microsoft.AspNetCore.OpenApi` 10.x (natif .NET 10, premier
  choix Microsoft)
- **UI** : `Scalar.AspNetCore` 2.x (recommandé par Microsoft, DX supérieure à Swagger UI)

Swashbuckle n'est pas utilisé : son auteur original l'a abandonné lors du cycle .NET 8,
et Microsoft fournit désormais une solution native de première partie.

## Installation

```csharp
[DependsOn(typeof(GranitApiDocumentationModule))]
public sealed class MyApplicationModule : GranitModule { }
```

Appeler `UseGranitApiDocumentation()` dans `Program.cs` **après** `app.Build()` :

```csharp
WebApplication app = builder.Build();

app.UseGranitApiDocumentation(); // → /openapi/v1.json et /scalar/v1

app.Run();
```

## Configuration

```json
{
  "ApiDocumentation": {
    "Title": "My API",
    "MajorVersions": [1],
    "Description": "API clinique — données sensibles ISO 27001",
    "ContactEmail": "api@digitaldynamics.be",
    "LogoUrl": "/logo.svg",
    "FaviconUrl": "/favicon.svg",
    "EnableInProduction": false
  }
}
```

Le logo est injecté dans le document OpenAPI via l'extension `x-logo` dans le bloc
`info`, et affiché dans la barre latérale de l'UI Scalar. Le favicon est configuré
via Scalar. Les deux acceptent une URL absolue ou un chemin relatif servi par
l'application.

| Option | Type | Défaut | Description |
| ------ | ---- | ------ | ----------- |
| `MajorVersions` | `int[]` | `[1]` | Versions à documenter — un document JSON par entrée |
| `Title` | `string` | `"API"` | Titre affiché dans l'UI Scalar |
| `Description` | `string?` | `null` | Description Markdown dans l'info block OpenAPI |
| `ContactEmail` | `string?` | `null` | Email de contact dans l'info block OpenAPI |
| `EnableInProduction` | `bool` | `false` | Expose l'UI Scalar en production si `true` |
| `EnableTenantHeader` | `bool` | `false` | Ajoute le header tenant comme paramètre requis |
| `TenantHeaderName` | `string` | `"X-Tenant-Id"` | Nom du header tenant dans la documentation |
| `AuthorizationPolicy` | `string?` | `null` | Policy d'autorisation sur les endpoints doc (voir ci-dessous) |
| `LogoUrl` | `string?` | `null` | URL du logo affiché dans la barre latérale Scalar et le bloc info OpenAPI (`x-logo`) |
| `FaviconUrl` | `string?` | `null` | URL du favicon de la page Scalar |

### Enregistrement direct (sans modules)

```csharp
builder.AddGranitApiDocumentation();
```

La méthode est une extension sur `IHostApplicationBuilder`. Les options sont lues
depuis la section `ApiDocumentation` de la configuration
(voir [appsettings.json ci-dessus](#configuration)).

## Endpoints générés

Pour `MajorVersions: [1, 2]`, les endpoints suivants sont créés :

- `/openapi/v1.json` — document OpenAPI version 1
- `/openapi/v2.json` — document OpenAPI version 2
- `/scalar/{documentName}` — UI Scalar avec sélecteur de version

## Compatibilité Wolverine HTTP

Le module supporte nativement les endpoints Wolverine HTTP en plus des contrôleurs MVC.
Tous les transformers (InternalApi, JWT Bearer, tenant header, réponses d'erreur,
normalisation Wolverine) fonctionnent de manière identique sur les deux types d'endpoints.

Wolverine expose ses endpoints via `IApiDescriptionProvider`, et les métadonnées
(`[Authorize]`, `[AllowAnonymous]`, attributs personnalisés) sont propagées dans
`EndpointMetadata`. Aucune configuration supplémentaire n'est nécessaire.

### Normalisation des métadonnées Wolverine

Le `WolverineOpenApiOperationTransformer` corrige automatiquement trois artefacts
que Wolverine HTTP génère dans les métadonnées OpenAPI :

| Artefact | Comportement Wolverine | Correction appliquée |
| -------- | ---------------------- | -------------------- |
| **operationId** | Auto-généré (`POST_api_v1_gdpr_export`) | Remplacé par le `Name` de l'attribut `[WolverinePost(..., Name = "RequestDataExport")]` via réflexion, ou par `EndpointNameMetadata` |
| **description** | Copie de l'operationId auto-généré | Supprimée quand elle correspond au pattern `METHOD_path_segments` |
| **réponse 200 fantôme** | Ajoutée avec un schéma `IResult` opaque, même si le endpoint déclare 202 ou 204 | Supprimée quand le endpoint déclare explicitement d'autres codes 2xx via `[ProducesResponseType]` |

Le transformer lit le `Name` de l'attribut Wolverine par réflexion pour éviter
une dépendance compile-time sur `Wolverine.Http`. Il distingue les
`ProducesResponseTypeAttribute` (déclarés par le développeur) des
`IProducesResponseTypeMetadata` (injectés automatiquement par Wolverine : 200 + 404).

> **Note** : Wolverine ajoute aussi une réponse 404 fantôme sur tous les endpoints.
> Le transformer RFC 7807 supprime automatiquement ces 404 sur les endpoints
> sans paramètre de route.
>
> **Voir aussi** : [http-responses.md](http-responses.md) pour les conventions
> sur les codes de retour HTTP (quand utiliser 200, 202, 204, etc.).

## Attribut `[InternalApi]`

L'attribut `[InternalApi]` exclut silencieusement un contrôleur ou une action de tous
les documents OpenAPI générés. Il fonctionne sur les contrôleurs MVC et les endpoints
Wolverine HTTP. Il est idéal pour :

- Les webhooks de réception d'événements
- Les endpoints de synchronisation inter-microservices
- Les routes d'administration brutes qui ne doivent pas figurer dans la documentation

### Application sur un contrôleur MVC

```csharp
using Granit.ApiDocumentation.Attributes;

[InternalApi]
[ApiController]
[Route("api/v{version:apiVersion}/internal/sync")]
public sealed class SyncController : ControllerBase
{
    [HttpPost]
    public IActionResult Sync() => Ok();
}
```

### Application sur une action spécifique

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/appointments")]
public sealed class AppointmentController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok();

    [InternalApi]
    [HttpPost("bulk-import")]
    public IActionResult BulkImport() => Ok(); // absent du document OpenAPI
}
```

### Application sur un endpoint Minimal API

```csharp
using Granit.ApiDocumentation.Attributes;

group.MapPost("/internal/sync", async (SyncCommand command, ISyncService service) =>
{
    await service.SyncAsync(command);
    return Results.NoContent();
})
.WithMetadata(new InternalApiAttribute());
```

L'attribut est ajouté aux métadonnées de l'endpoint via `.WithMetadata()`, ce qui est
détecté automatiquement par le `InternalApiDocumentTransformer` (même mécanisme que
pour les contrôleurs MVC et les endpoints Wolverine HTTP).

> **Note** : pour exclure **tous** les endpoints d'un groupe, appliquer la métadonnée
> sur le `RouteGroupBuilder` :
>
> ```csharp
> RouteGroupBuilder internalGroup = group.MapGroup("/internal")
>     .WithMetadata(new InternalApiAttribute());
>
> internalGroup.MapPost("/sync", SyncEndpoint.HandleAsync);
> internalGroup.MapPost("/purge", PurgeEndpoint.HandleAsync);
> ```

### Application sur un endpoint Wolverine HTTP

```csharp
using Granit.ApiDocumentation.Attributes;

public static class InternalSyncEndpoint
{
    [InternalApi]
    [WolverinePost("/api/v1/internal/sync")]
    public static SyncResponse Post(SyncCommand command) => new();
}
```

## Header multi-tenant

Quand `EnableTenantHeader = true`, le module ajoute automatiquement un paramètre de
header requis (`X-Tenant-Id` par défaut) sur toutes les opérations OpenAPI. Cela
permet aux consommateurs de l'API de voir le header tenant dans la documentation
et de l'envoyer depuis l'UI Scalar.

```json
{
  "ApiDocumentation": {
    "EnableTenantHeader": true,
    "TenantHeaderName": "X-Tenant-Id"
  }
}
```

### Exclure un endpoint du header tenant

L'attribut `[AllowAnonymousTenant]` (dans `Granit.Core.MultiTenancy`) exclut un
endpoint du header tenant dans la documentation. Cela suit le pattern de dépendance
souple : l'attribut est disponible partout via `Granit.Core` sans dépendance dure
sur `Granit.MultiTenancy`.

```csharp
using Granit.Core.MultiTenancy;

public static class HealthCheckEndpoint
{
    [AllowAnonymousTenant]
    [WolverineGet("/api/v1/health")]
    public static HealthResponse Get() => new();
}
```

## Réponses d'erreur RFC 7807

Le module ajoute automatiquement des réponses d'erreur
`application/problem+json` (RFC 7807) sur les opérations OpenAPI en fonction
des métadonnées de l'endpoint :

| Code | Condition | Description |
| ---- | --------- | ----------- |
| 401 | `[Authorize]` sans `[AllowAnonymous]` | Non authentifié |
| 403 | `[Authorize]` sans `[AllowAnonymous]` | Non autorisé |
| 422 | Opération avec un corps de requête | Erreur de validation |
| 500 | Toujours | Erreur interne du serveur |

Chaque réponse d'erreur inclut un schéma inline RFC 7807 (`type`, `title`,
`status`, `detail`, `instance`) avec le content-type `application/problem+json`.
Les réponses existantes ne sont pas écrasées. Si l'opération définit déjà une
réponse 500 personnalisée, le transformer la conserve.

### Nettoyage des 404 fantômes Wolverine

Wolverine ajoute une réponse 404 sur tous les endpoints, y compris ceux qui n'ont
pas de paramètre de route. Le transformer supprime automatiquement ces 404 fantômes
sur les endpoints sans paramètre `{id}` dans la route, tout en les conservant sur les
endpoints avec paramètre de route (où un 404 est légitime).

## Intégration JWT Bearer

Si `GranitJwtBearerModule` (ou tout schéma `JwtBearerDefaults.AuthenticationScheme`)
est enregistré dans l'application, le transformer JWT ajoute automatiquement :

- La définition de sécurité `Bearer` dans les `securitySchemes` du document
- Une **exigence de sécurité globale** au niveau du document (`document.Security`)

Le bouton **Authorize** apparaît alors dans l'UI Scalar sans configuration supplémentaire.

Si l'application n'utilise pas de JWT Bearer, aucune définition de sécurité n'est ajoutée.
Le transformer vérifie dynamiquement la présence du schéma au démarrage.

### Modèle de sécurité OpenAPI (global + override)

La sécurité est gérée en deux niveaux, conformément à la spécification OpenAPI 3.1 :

1. **Niveau document** (`JwtBearerSecuritySchemeTransformer`) : déclare une exigence
   globale Bearer (ou OAuth2 si configuré). Toutes les opérations héritent de cette
   exigence par défaut.
2. **Niveau opération** (`SecurityRequirementOperationTransformer`) : ajuste chaque
   endpoint individuellement :
   - **`[AllowAnonymous]`** → `security: [{}]` (aucune authentification requise,
     override explicite de la sécurité globale)
   - **Endpoints protégés** → `security: null` (héritage de la sécurité globale)

Sans ce mécanisme, ASP.NET Core génère `security: [{}]` sur toutes les opérations,
ce qui signifie « pas d'authentification requise » dans OpenAPI, indépendamment des
attributs `[Authorize]` réels.

## Intégration OAuth2

Quand la configuration OAuth2 est renseignée, le module remplace automatiquement
le schéma HTTP Bearer par un schéma OAuth2 Authorization Code dans le document
OpenAPI. Cela permet l'authentification interactive directement depuis l'UI Scalar.

**Aucun couplage** entre `Granit.ApiDocumentation` et `Granit.Authentication.Keycloak` :
les URLs OAuth2 sont configurées indépendamment dans `appsettings.json`.

### Configuration OAuth2

```json
{
  "ApiDocumentation": {
    "Title": "My API",
    "OAuth2": {
      "AuthorizationUrl": "https://keycloak.example.com/realms/my-realm/protocol/openid-connect/auth",
      "TokenUrl": "https://keycloak.example.com/realms/my-realm/protocol/openid-connect/token",
      "ClientId": "my-frontend",
      "Scopes": ["openid", "profile"]
    }
  }
}
```

| Option | Type | Défaut | Description |
| ------ | ---- | ------ | ----------- |
| `AuthorizationUrl` | `string?` | `null` | URL d'autorisation OAuth2 |
| `TokenUrl` | `string?` | `null` | URL du token OAuth2 |
| `ClientId` | `string?` | `null` | Client ID **public** (frontend, PKCE) |
| `EnablePkce` | `bool` | `true` | Active PKCE (S256) dans le flow Authorization Code |
| `Scopes` | `string[]` | `["openid"]` | Scopes OAuth2 à demander |

Les trois premières options (`AuthorizationUrl`, `TokenUrl`, `ClientId`) sont
obligatoires. Si l'une d'entre elles est absente ou vide, le schéma OAuth2 n'est
pas activé et le schéma Bearer reste en place (backward-compatible).

> **Important** : `OAuth2.ClientId` est le client **public** (frontend) qui
> supporte PKCE, pas le client confidentiel du backend. Pour Keycloak, c'est
> typiquement un client distinct avec **Standard flow** activé et **Client
> authentication** désactivé.

### Comportement des transformers

Trois transformers de sécurité s'exécutent dans cet ordre :

| # | Transformer | Niveau | Rôle |
| - | ----------- | ------ | ---- |
| 1 | `JwtBearerSecuritySchemeTransformer` | Document | Ajoute le schéma Bearer + exigence globale |
| 2 | `OAuth2SecuritySchemeTransformer` | Document | Remplace Bearer par OAuth2 (si configuré) |
| 3 | `SecurityRequirementOperationTransformer` | Opération | Override par endpoint (`[AllowAnonymous]` → pas d'auth) |

Si OAuth2 n'est pas configuré, le transformer 2 est un no-op et le schéma
Bearer reste. Si JWT Bearer n'est pas enregistré, les transformers 1 et 2
sont tous les deux des no-ops. Le transformer 3 s'exécute toujours.

### UI Scalar

Quand OAuth2 est configuré, l'UI Scalar affiche un bouton **Authorize** qui
déclenche un flow Authorization Code avec PKCE. L'utilisateur est redirigé vers
l'IDP (Keycloak), s'authentifie, et le token est automatiquement injecté dans
les requêtes suivantes.

La configuration Scalar est automatique via `AddAuthorizationCodeFlow()` dans
`UseGranitApiDocumentation()`.

## Génération de clients typés

Les documents OpenAPI générés par Granit (`/openapi/v1.json`) peuvent alimenter
des outils de codegen pour produire des clients HTTP typés. Cela élimine le code
fetch/HttpClient écrit à la main et garantit la synchronisation client-serveur.

### Clients TypeScript (frontend)

#### orval (recommandé)

[orval](https://orval.dev/) génère des hooks TanStack Query (React Query) typés
directement depuis le document OpenAPI. Chaque endpoint produit un hook prêt à
l'emploi.

Installation :

```bash
npm install -D orval
```

Configuration `orval.config.ts` :

```typescript
import { defineConfig } from "orval";

export default defineConfig({
  myApi: {
    input: {
      target: "http://localhost:5000/openapi/v1.json",
    },
    output: {
      target: "./src/api/generated.ts",
      client: "react-query",
      mode: "tags-split",
      override: {
        mutator: {
          path: "./src/api/custom-fetch.ts",
          name: "customFetch",
        },
        header: (info) => [
          "/* eslint-disable */",
          `/* Generated from OpenAPI spec — ${info.title} ${info.version} */`,
        ],
      },
    },
  },
});
```

Génération :

```bash
npx orval
```

Le `customFetch` doit injecter le header `X-Tenant-Id` si le multi-tenant est actif :

```typescript
// src/api/custom-fetch.ts
export const customFetch = async <T>(url: string, options: RequestInit): Promise<T> => {
  const tenantId = getTenantId(); // depuis le contexte applicatif
  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      ...(tenantId && { "X-Tenant-Id": tenantId }),
    },
  });
  if (!response.ok) throw response;
  return response.json();
};
```

#### openapi-typescript (alternative légère)

[openapi-typescript](https://openapi-ts.dev/) génère uniquement des types TypeScript,
sans code runtime. Idéal si le projet n'utilise pas React ou préfère un contrôle
total sur les appels HTTP.

```bash
npx openapi-typescript http://localhost:5000/openapi/v1.json -o ./src/api/schema.d.ts
```

Les types générés s'utilisent avec `openapi-fetch` pour un client typé minimal :

```typescript
import createClient from "openapi-fetch";
import type { paths } from "./schema";

const client = createClient<paths>({ baseUrl: "http://localhost:5000" });

const { data, error } = await client.GET("/api/v1/patients/{id}", {
  params: { path: { id: "abc-123" } },
});
```

### Clients C# (inter-microservices)

#### Kiota (recommandé)

[Kiota](https://learn.microsoft.com/fr-fr/openapi/kiota/overview) est l'outil
Microsoft de génération de clients HTTP depuis OpenAPI. Il produit des request
builders granulaires avec un typage fort.

Installation en outil global :

```bash
dotnet tool install -g Microsoft.OpenApi.Kiota
```

Génération :

```bash
kiota generate \
  --openapi http://localhost:5000/openapi/v1.json \
  --language CSharp \
  --output ./Generated/PatientService \
  --namespace-name MyApp.Clients.PatientService \
  --class-name PatientServiceClient
```

Pour automatiser la régénération au build, ajouter un Target MSBuild dans le
`.csproj` du projet consommateur :

```xml
<Target Name="GenerateApiClient" BeforeTargets="CoreCompile"
        Inputs="$(OpenApiSpec)" Outputs="$(GeneratedDir)/.timestamp">
  <Exec Command="kiota generate
    --openapi $(OpenApiSpec)
    --language CSharp
    --output $(GeneratedDir)
    --namespace-name $(RootNamespace).Clients
    --class-name ApiClient
    --clean-output" />
  <Touch Files="$(GeneratedDir)/.timestamp" AlwaysCreate="true" />
</Target>
```

> **Note** : NSwag est déprécié de facto (maintenance minimale depuis 2024). Préférer
> Kiota pour tout nouveau projet.

### Bonnes pratiques

- **Générer depuis le document servi** : pointer vers l'URL de l'API
  (`/openapi/v1.json`), pas vers un fichier JSON copié manuellement. Cela garantit
  que le client reflète toujours l'état réel de l'API.
- **Régénérer en CI** : ajouter une étape dans le pipeline qui régénère le client
  et échoue si les types changent sans mise à jour du consommateur.
- **Ne pas committer le code généré** : ajouter le répertoire de sortie dans
  `.gitignore` et régénérer au build. Si le projet l'exige, marquer les fichiers
  avec `linguist-generated=true` dans `.gitattributes`.
- **Header tenant** : si `EnableTenantHeader = true`, le header `X-Tenant-Id`
  apparaît dans le document OpenAPI et les clients générés l'incluront dans
  leurs signatures. Configurer l'injection du tenant dans le HTTP handler
  (middleware DelegatingHandler en C#, intercepteur fetch en TypeScript).

## Exemples de schéma pour Scalar Try-it

Le module fournit un mécanisme distribué pour injecter des exemples JSON réalistes
dans les schémas OpenAPI. L'UI Scalar pré-remplit alors les formulaires **Try it**
sur les endpoints POST/PUT, évitant la saisie manuelle.

### Architecture

Le mécanisme repose sur trois éléments :

1. **`ISchemaExampleProvider`** — interface dans `Granit.ApiDocumentation` que chaque
   package `*.Endpoints` implémente pour ses propres types Request
2. **`SchemaExampleSchemaTransformer`** — transformer `IOpenApiSchemaTransformer` qui
   collecte tous les providers via DI et applique `example` sur les schémas correspondants
3. **Auto-discovery** — `AddGranitApiDocumentation()` scanne automatiquement les
   assemblies chargées pour trouver les implémentations de `ISchemaExampleProvider`
   et les enregistre en DI (aucun enregistrement manuel nécessaire)

### Implémentation d'un provider

Chaque package `*.Endpoints` crée une classe `internal sealed` implémentant
`ISchemaExampleProvider`. Les exemples utilisent `JsonObject` (`System.Text.Json.Nodes`)
pour rester découplés de `Microsoft.OpenApi` :

```csharp
using System.Text.Json.Nodes;
using Granit.ApiDocumentation;

namespace Granit.ReferenceData.Endpoints;

internal sealed class ReferenceDataSchemaExampleProvider : ISchemaExampleProvider
{
    public IReadOnlyDictionary<Type, JsonNode> GetExamples() =>
        new Dictionary<Type, JsonNode>
        {
            [typeof(ReferenceDataCreateRequest)] = new JsonObject
            {
                ["code"] = "BE",
                ["labelEn"] = "Belgium",
                ["labelFr"] = "Belgique",
                ["sortOrder"] = 56,
            },
        };
}
```

### Contraintes

- Les DTOs restent des POCOs/records purs — pas d'attribut Swagger ni de dépendance
  à `Microsoft.OpenApi`
- Les exemples ne doivent contenir **aucune PII** ni donnée patient
- Les `JsonNode` sont deep-clonés automatiquement par le transformer pour éviter
  les mutations partagées entre schémas

### Packages avec providers

| Package | Types couverts |
| ------- | -------------- |
| `Granit.ReferenceData.Endpoints` | `ReferenceDataCreateRequest`, `ReferenceDataUpdateRequest` |
| `Granit.DataExchange.Endpoints` | `ConfirmMappingsRequest`, `CreateExportJobRequest`, `SaveExportPresetRequest` |
| `Granit.Notifications.Endpoints` | `UpdatePreferenceRequest` |
| `Granit.Querying.Endpoints` | `CreateSavedViewRequest`, `UpdateSavedViewRequest` |
| `Granit.Workflow.Endpoints` | `WorkflowTransitionRequest` |
| `Granit.Localization.Endpoints` | `SetLocalizationOverrideRequest` |
| `Granit.Identity.Endpoints` | `IdentityUserCacheBatchRequest`, `IdentityUserCacheSyncRequest`, `IdentityWebhookPayload` |
| `Granit.Templating.Endpoints` | `SaveTemplateRequest` |

Les packages `*.Endpoints` sans type Request (Authorization, BackgroundJobs, Cookies,
Timeline) n'ont pas de provider — c'est attendu.

## Considérations ISO 27001

### UI en production

Par défaut, `EnableInProduction = false` : le document OpenAPI et l'UI Scalar
sont désactivés en production, conformément aux recommandations de sécurité Microsoft.

Si une documentation interne est nécessaire en production (portail développeur interne),
activer `EnableInProduction = true` et configurer `AuthorizationPolicy` pour
protéger l'accès.

### Protection des endpoints de documentation

L'option `AuthorizationPolicy` contrôle l'accès aux endpoints `/openapi/v*.json`
et `/scalar/*` :

| Valeur | Comportement |
| ------ | ------------ |
| `null` (défaut) | Aucune policy — hérite du comportement global de l'application |
| `""` (chaîne vide) | Accès anonyme explicite (`.AllowAnonymous()`) |
| `"PolicyName"` | Accès protégé par la policy nommée (`.RequireAuthorization()`) |

#### Portail développeur interne (ISO 27001)

```json
{
  "ApiDocumentation": {
    "EnableInProduction": true,
    "AuthorizationPolicy": "InternalDeveloper"
  }
}
```

Seuls les utilisateurs authentifiés avec la policy `InternalDeveloper` accèdent
à la documentation. Le document OpenAPI et l'UI Scalar retournent 401/403 sinon.

#### API publique avec fallback policy globale

Si l'application a une fallback authorization policy (toutes les routes protégées
par défaut) mais que la documentation doit rester accessible :

```json
{
  "ApiDocumentation": {
    "AuthorizationPolicy": ""
  }
}
```

La chaîne vide applique `.AllowAnonymous()` explicitement sur les endpoints de
documentation, contournant la fallback policy.

### Endpoints internes

Utiliser `[InternalApi]` pour tous les endpoints qui ne doivent pas figurer dans
la documentation publique. Cela réduit la surface d'attaque exposée dans la documentation
et évite de révéler l'architecture interne aux auditeurs externes.

## Dépendances Granit

| Direction       | Modules                                        |
| --------------- | ---------------------------------------------- |
| **Dépend de**   | `Granit.ApiVersioning`, `Granit.Security`      |
| **Utilisé par** | Module feuille (consommé par les applications) |

> Voir le [graphe de dépendances complet](../dependencies.md).
