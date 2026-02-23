# API Versioning

`Granit.ApiVersioning` gère le versioning HTTP pour toutes les
APIs Digital Dynamics. Il peut être utilisé seul dans les services inter-services
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

## Considérations HDS

La stratégie URL (`/api/v1/patients`) garantit que la version de l'API figure
systématiquement dans les access logs OVHcloud. Les logs d'audit HDS (3 ans de rétention)
peuvent ainsi identifier avec précision quelle version de l'API a traité une requête.

Le fallback query string (`?api-version=1.0`) est également visible dans les access logs,
contrairement aux headers HTTP souvent omis par les reverse proxies.
