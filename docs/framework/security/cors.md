# CORS

`Granit.Cors` fournit une configuration CORS standardisée et conforme HDS
pour les applications Granit. Les origines autorisées sont déclarées dans
`appsettings.json` et validées au démarrage.

## Installation

```bash
dotnet add package Granit.Cors
```

## Configuration rapide

### Via le système de modules

```csharp
[DependsOn(typeof(GranitCorsModule))]
public sealed class AppModule : GranitModule { }
```

### Enregistrement direct

```csharp
builder.AddGranitCors();
```

L'application doit aussi appeler `app.UseCors()` dans le pipeline HTTP.

## appsettings.json

```json
{
  "Cors": {
    "AllowedOrigins": ["https://app.example.com", "https://admin.example.com"],
    "AllowCredentials": false
  }
}
```

### Propriétés

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `AllowedOrigins` | `string[]` | `[]` (requis) | Origines autorisées. Au moins une obligatoire. |
| `AllowCredentials` | `bool` | `false` | Inclure `Access-Control-Allow-Credentials: true`. |

## Règles de validation HDS

Le module valide la configuration au démarrage et échoue immédiatement
(fail-fast) si une règle est violée :

| Règle | Environnement | Description |
| --- | --- | --- |
| Pas de wildcard `*` | Non-Development | Interdit en production, staging, etc. |
| Credentials + wildcard | Tous | Interdit (violation spécification CORS) |
| Origines vides | Tous | Au moins une origine requise |

## Politique par défaut

La politique CORS par défaut applique :

- **`AllowAnyHeader()`** — tous les en-têtes HTTP sont autorisés
- **`AllowAnyMethod()`** — GET, POST, PUT, DELETE, PATCH, etc.
- Origines restreintes aux valeurs de `AllowedOrigins`
- `AllowCredentials()` activé uniquement si `AllowCredentials = true`

## Environnement de développement

En développement, le wildcard `*` est autorisé pour simplifier le setup
local :

```json
{
  "Cors": {
    "AllowedOrigins": ["*"]
  }
}
```

Cette configuration est **rejetée** dans tout environnement autre que
Development.

## Pipeline HTTP

Le module enregistre les services CORS mais ne configure pas le middleware.
L'application doit appeler `app.UseCors()` dans son pipeline :

```csharp
WebApplication app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();
```

## Dépendances Granit

| Package | Dépend de | Utilisé par |
| --- | --- | --- |
| `Granit.Cors` | `Granit.Core` | Module feuille (applications) |
