# API Documentation

`Granit.ApiDocumentation` génère les documents OpenAPI et expose
l'UI Scalar multi-version pour toutes les APIs Digital Dynamics. Il dépend de
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
    "Title": "Guava API",
    "MajorVersions": [1],
    "Description": "API clinique Guava — données de santé HDS",
    "ContactEmail": "api@digitaldynamics.be",
    "EnableInProduction": false
  }
}
```

| Option | Type | Défaut | Description |
| ------ | ---- | ------ | ----------- |
| `MajorVersions` | `int[]` | `[1]` | Versions à documenter — un document JSON par entrée |
| `Title` | `string` | `"API"` | Titre affiché dans l'UI Scalar |
| `Description` | `string?` | `null` | Description Markdown dans l'info block OpenAPI |
| `ContactEmail` | `string?` | `null` | Email de contact dans l'info block OpenAPI |
| `EnableInProduction` | `bool` | `false` | Expose l'UI Scalar en production si `true` |
| `EnableTenantHeader` | `bool` | `false` | Ajoute le header tenant comme paramètre requis |
| `TenantHeaderName` | `string` | `"X-Tenant-Id"` | Nom du header tenant dans la documentation |

### Configuration programmatique

Quand les versions ou les métadonnées doivent être définies en code :

```csharp
builder.Services.AddGranitApiDocumentation(opts =>
{
    opts.Title = "Guava API";
    opts.MajorVersions = [1, 2];
    opts.ContactEmail = "api@digitaldynamics.be";
});
```

## Endpoints générés

Pour `MajorVersions: [1, 2]`, les endpoints suivants sont créés :

- `/openapi/v1.json` — document OpenAPI version 1
- `/openapi/v2.json` — document OpenAPI version 2
- `/scalar/{documentName}` — UI Scalar avec sélecteur de version

## Compatibilité Wolverine HTTP

Le module supporte nativement les endpoints Wolverine HTTP en plus des contrôleurs MVC.
Tous les transformers (InternalApi, JWT Bearer, tenant header, réponses d'erreur)
fonctionnent de manière identique sur les deux types d'endpoints.

Wolverine expose ses endpoints via `IApiDescriptionProvider`, et les métadonnées
(`[Authorize]`, `[AllowAnonymous]`, attributs personnalisés) sont propagées dans
`EndpointMetadata`. Aucune configuration supplémentaire n'est nécessaire.

> **Note** : Wolverine ajoute une réponse 404 fantôme sur tous les endpoints.
> Le transformer RFC 7807 supprime automatiquement ces 404 sur les endpoints
> sans paramètre de route.

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

- La définition de sécurité `Bearer` dans le document OpenAPI
- L'exigence de sécurité Bearer sur toutes les opérations

Le bouton **Authorize** apparaît alors dans l'UI Scalar sans configuration supplémentaire.

Si l'application n'utilise pas de JWT Bearer, aucune définition de sécurité n'est ajoutée.
Le transformer vérifie dynamiquement la présence du schéma au démarrage.

## Considérations HDS

### UI en production

Par défaut, `EnableInProduction = false` : le document OpenAPI et l'UI Scalar
sont désactivés en production, conformément aux recommandations de sécurité Microsoft.

Si une documentation interne est nécessaire en production (portail développeur interne),
activer `EnableInProduction = true` en s'assurant que l'endpoint Scalar est protégé
par un middleware d'authentification.

### Endpoints internes

Utiliser `[InternalApi]` pour tous les endpoints qui ne doivent pas figurer dans
la documentation publique. Cela réduit la surface d'attaque exposée dans la documentation
et évite de révéler l'architecture interne aux auditeurs externes.
