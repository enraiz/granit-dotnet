# API Versioning & Documentation

## Vue d'ensemble

Deux packages Foundation assurent le versioning d'API et la documentation OpenAPI
pour toutes les applications Digital Dynamics.

| Package | Rôle |
| ------- | ---- |
| `DigitalDynamics.Foundation.ApiVersioning` | Versioning HTTP par URL et query string |
| `DigitalDynamics.Foundation.ApiDocumentation` | Documentation OpenAPI et UI Scalar multi-version |

Ces packages sont indépendants : `ApiVersioning` peut être utilisé seul dans les APIs
inter-services qui n'ont pas besoin d'une UI de documentation.

## Choix technologiques

- **Versioning** : `Asp.Versioning.Mvc` 9.x (standard .NET, remplaçant officiel de l'ancien package)
- **Génération OpenAPI** : `Microsoft.AspNetCore.OpenApi` 10.x (natif .NET 10, premier choix Microsoft)
- **UI** : `Scalar.AspNetCore` 2.x (recommandé par Microsoft, DX supérieure à Swagger UI)

Swashbuckle n'est pas utilisé : son auteur original l'a abandonné lors du cycle .NET 8,
et Microsoft fournit désormais une solution native de première partie.

## Foundation.ApiVersioning

### Installation

```csharp
[DependsOn(typeof(FoundationApiVersioningModule))]
public sealed class MyApplicationModule : FoundationModule { }
```

### Configuration

```json
{
  "ApiVersioning": {
    "DefaultMajorVersion": 1,
    "ReportApiVersions": true
  }
}
```

| Option | Type | Défaut | Description |
| ------ | ---- | ------ | ----------- |
| `DefaultMajorVersion` | `int` | `1` | Version assumée si le client ne la précise pas |
| `ReportApiVersions` | `bool` | `true` | Ajoute les headers `api-supported-versions` et `api-deprecated-versions` |

### Versioning des routes

Le template de route recommandé est `/api/v{version:apiVersion}/resource`.

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/patients")]
[ApiVersion("1.0")]
public sealed class PatientController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok();
}
```

La version peut être spécifiée de deux façons par les clients :

- **URL** (recommandé) : `GET /api/v1/patients`
- **Query string** (fallback) : `GET /api/patients?api-version=1.0`

Le header `X-Api-Version` n'est pas supporté intentionnellement : les headers sont souvent
omis des access logs et ne garantissent pas la traçabilité dans les audits HDS.

## Foundation.ApiDocumentation

### Installation

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

### Configuration

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

### Endpoints générés

Pour `MajorVersions: [1, 2]`, les endpoints suivants sont créés :

- `/openapi/v1.json` — document OpenAPI version 1
- `/openapi/v2.json` — document OpenAPI version 2
- `/scalar/{documentName}` — UI Scalar avec sélecteur de version

### Configuration programmatique

Quand les versions ou les métadonnées doivent être définies en code (par exemple,
injectées depuis un secret Vault) :

```csharp
builder.Services.AddFoundationApiDocumentation(opts =>
{
    opts.Title = "Guava API";
    opts.MajorVersions = [1, 2];
    opts.ContactEmail = "api@digitaldynamics.be";
});
```

## Attribut `[InternalApi]`

L'attribut `[InternalApi]` exclut silencieusement un contrôleur ou une action de tous
les documents OpenAPI générés. Il est idéal pour :

- Les webhooks de réception d'événements
- Les endpoints de synchronisation inter-microservices
- Les routes d'administration brutes qui ne doivent pas figurer dans la documentation

### Application sur un contrôleur entier

```csharp
using DigitalDynamics.Foundation.ApiDocumentation.Attributes;

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

### Versioning dans les logs d'audit

La stratégie URL (`/api/v1/patients`) garantit que la version de l'API figure
systématiquement dans les access logs OVHcloud. Les logs d'audit HDS (3 ans de rétention)
peuvent ainsi identifier avec précision quelle version de l'API a traité une requête.

Le fallback query string (`?api-version=1.0`) est également visible dans les access logs,
contrairement aux headers HTTP souvent omis par les reverse proxies.

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
