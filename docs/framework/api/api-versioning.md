# API Versioning

`Granit.ApiVersioning` gère le versioning HTTP pour toutes les
APIs Granit. Il peut être utilisé seul dans les services inter-services
qui n'ont pas besoin d'une UI de documentation.

> **Voir aussi** : [api-documentation.md](api-documentation.md) pour la génération
> OpenAPI et l'UI Scalar multi-version.

## Choix technologique

- **Versioning** : `Asp.Versioning.Mvc` 9.x (standard .NET, remplaçant officiel de
  l'ancien package Microsoft)

## Installation

```csharp
[DependsOn(typeof(GranitApiVersioningModule))]
public sealed class MyApplicationModule : GranitModule { }
```

## Configuration

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

## Versioning des routes

Le template de route recommandé est `/api/v{version:apiVersion}/resource`.

La version peut être spécifiée de deux façons par les clients :

- **URL** (recommandé) : `GET /api/v1/patients`
- **Query string** (fallback) : `GET /api/patients?api-version=1.0`

Le header `X-Api-Version` n'est pas supporté intentionnellement : les headers sont souvent
omis des access logs et ne garantissent pas la traçabilité dans les audits ISO 27001.

### Minimal API (recommandé)

Le pattern standard Granit crée un `ApiVersionSet` au niveau de `Program.cs` et enregistre
tous les endpoints sous un groupe versionné :

```csharp
// 1. Déclarer les versions supportées
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

// 2. Créer le groupe racine versionné
var api = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

// 3. Enregistrer les endpoints — ils héritent du versioning automatiquement
api.MapBackgroundJobsEndpoints();
api.MapTimelineEndpoints();
```

Tous les endpoints enregistrés sur le groupe `api` héritent automatiquement de la version.
Aucun attribut `[ApiVersion]` n'est nécessaire.

#### Plusieurs versions (v1 + v2)

Pour exposer simultanément deux versions majeures :

```csharp
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();

var api = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

// Endpoint disponible en v1 et v2 (comportement identique)
api.MapGet("/patients", GetAllPatients);

// Endpoint disponible uniquement en v2
api.MapGet("/patients/summary", GetPatientsSummary)
    .MapToApiVersion(2);

// Deux implémentations différentes selon la version
api.MapGet("/patients/{id}", GetPatientV1).MapToApiVersion(1);
api.MapGet("/patients/{id}", GetPatientV2).MapToApiVersion(2);
```

`.MapToApiVersion()` restreint un endpoint à une version spécifique. Sans cet appel,
l'endpoint est disponible sur toutes les versions déclarées dans le `ApiVersionSet`.

#### Dépréciation d'une version

```csharp
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .HasDeprecatedApiVersion(new ApiVersion(1))
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();
```

`.HasDeprecatedApiVersion()` ajoute la version au header `api-deprecated-versions` dans
la réponse. Les clients reçoivent un signal clair que la v1 est dépréciée et qu'ils
doivent migrer vers la v2.

### Contrôleur MVC

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

## Considérations ISO 27001

La stratégie URL (`/api/v1/patients`) garantit que la version de l'API figure
systématiquement dans les access logs OVHcloud. Les logs d'audit ISO 27001 (3 ans de rétention)
peuvent ainsi identifier avec précision quelle version de l'API a traité une requête.

Le fallback query string (`?api-version=1.0`) est également visible dans les access logs,
contrairement aux headers HTTP souvent omis par les reverse proxies.

## Dépendances Granit

| Direction       | Modules                    |
| --------------- | -------------------------- |
| **Dépend de**   | `Granit.Core`              |
| **Utilisé par** | `Granit.ApiDocumentation`  |

> Voir le [graphe de dépendances complet](../dependencies.md).
