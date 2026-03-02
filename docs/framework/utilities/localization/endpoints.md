# Endpoint HTTP — Granit.Localization.Endpoints

Package complémentaire qui expose les traductions via HTTP pour les clients SPA
(Angular, Blazor, React). Un seul appel retourne toutes les ressources d'une culture
et la liste des langues disponibles.

## Installation

```bash
dotnet add package Granit.Localization.Endpoints
```

## Activation

Appeler `MapGranitLocalization()` après `app.Build()` :

```csharp
[DependsOn(typeof(GranitLocalizationEndpointsModule))]
public sealed class MyAppModule : GranitModule { }

// Dans Program.cs
app.MapGranitLocalization();
```

Sans le système de modules :

```csharp
builder.Services.AddGranitLocalization();

var app = builder.Build();
app.MapGranitLocalization();
```

## Personnalisation du préfixe de route

Le préfixe de route est configurable via `LocalizationEndpointsOptions`. Deux propriétés
permettent de composer l'URL finale :

| Option | Par défaut | Description |
| --- | --- | --- |
| `ApiPrefix` | *(vide)* | Préfixe API prépendé au `RoutePrefix` (ex. `api/v1`) |
| `RoutePrefix` | `localization` | Segment de domaine |
| `TagName` | `Localization` | Tag OpenAPI |

```csharp
// Ajouter un segment de version via ApiPrefix
app.MapGranitLocalization(opts => opts.ApiPrefix = "api/v1");
app.MapGranitLocalizationOverrides(opts => opts.ApiPrefix = "api/v1");
```

Le préfixe effectif est calculé : `{ApiPrefix}/{RoutePrefix}`. Sans `ApiPrefix`,
seul le `RoutePrefix` est utilisé. Les deux méthodes acceptent la même option
indépendamment, ce qui permet de versionner l'un sans l'autre.

## Endpoint

```text
GET /{prefix}?cultureName={culture}
```

Par défaut (sans `ApiPrefix`) :

```text
GET /localization?cultureName={culture}
```

Avec `ApiPrefix = "api/v1"` :

```text
GET /api/v1/localization?cultureName={culture}
```

| Paramètre | Obligatoire | Description |
| --- | --- | --- |
| `cultureName` | Non | Culture BCP 47 (`fr`, `en`, `fr-CA`). Fallback sur `CultureInfo.CurrentUICulture` si absent. |

L'endpoint est **anonyme** — les chaînes de traduction sont des données publiques.

## Réponse

```json
{
  "cultureName": "fr",
  "resources": {
    "MyApp": {
      "Patient:NotFound": "Patient introuvable.",
      "Validation.Required": "Ce champ est obligatoire."
    },
    "Granit": {
      "Granit:EntityNotFound": "L'entité de type {0} avec l'identifiant {1} n'a pas été trouvée."
    }
  },
  "languages": [
    { "cultureName": "fr", "displayName": "Français", "flagIcon": "fr", "isDefault": true },
    { "cultureName": "en", "displayName": "English",  "flagIcon": "gb", "isDefault": false }
  ]
}
```

`resources` est indexé par le nom court de la ressource (`[LocalizationResourceName]`
ou `Type.Name`). Les clés internes sont celles retournées par `IStringLocalizer.GetAllStrings()`.

## Caching HTTP

La réponse inclut systématiquement :

```http
Cache-Control: public, max-age=3600
Vary: Accept-Language
```

Ces headers permettent au navigateur et aux CDN de mettre en cache les traductions
par culture pendant une heure.

## Erreurs

| Code | Cause |
| --- | --- |
| `400` | `cultureName` ne respecte pas le format BCP 47 (ex. `en:invalid`) |

La validation est effectuée par regex avant toute résolution de culture, garantissant
un comportement identique sur Linux (ICU) et Windows (NLS).

## Dépendances

| Package | Rôle |
| --- | --- |
| `Microsoft.AspNetCore.App` | `IEndpointRouteBuilder`, `IResult`, `HttpContext` |
| `Granit.Localization` | `GranitLocalizationOptions`, `IStringLocalizerFactory` |
