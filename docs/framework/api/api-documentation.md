# API Documentation

`Granit.ApiDocumentation` génère les documents OpenAPI et expose
l'UI Scalar multi-version pour toutes les APIs Digital Dynamics. Il dépend de
`Foundation.ApiVersioning`.

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
[DependsOn(typeof(FoundationApiDocumentationModule))]
public sealed class MyApplicationModule : FoundationModule { }
```

Appeler `UseFoundationApiDocumentation()` dans `Program.cs` **après** `app.Build()` :

```csharp
WebApplication app = builder.Build();

app.UseFoundationApiDocumentation(); // → /openapi/v1.json et /scalar/v1

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

### Configuration programmatique

Quand les versions ou les métadonnées doivent être définies en code :

```csharp
builder.Services.AddFoundationApiDocumentation(opts =>
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

## Attribut `[InternalApi]`

L'attribut `[InternalApi]` exclut silencieusement un contrôleur ou une action de tous
les documents OpenAPI générés. Il est idéal pour :

- Les webhooks de réception d'événements
- Les endpoints de synchronisation inter-microservices
- Les routes d'administration brutes qui ne doivent pas figurer dans la documentation

### Application sur un contrôleur entier

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

## Intégration JWT Bearer

Si `FoundationJwtBearerModule` (ou tout schéma `JwtBearerDefaults.AuthenticationScheme`)
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
