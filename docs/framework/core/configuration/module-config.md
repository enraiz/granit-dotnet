# Module Config — IModuleConfigProvider\<T\>

Pattern standardisé pour exposer la configuration d'un module Granit aux clients
(frontend, applications tierces) via un endpoint `GET /{module}/config`.

## Pourquoi

Les frontends ont besoin de connaître certaines valeurs de configuration du backend
pour adapter leur UI (ex. : activer un bouton « Rejouer » si `StorePayload` est activé,
afficher les cookies enregistrés pour le CMP). Exposer `IOptions<T>` directement
violerait l'encapsulation et exposerait des détails internes.

`IModuleConfigProvider<T>` résout ce problème en imposant une projection explicite :
le module choisit quelles valeurs exposer et sous quelle forme.

## Architecture

```text
Granit.Core.Modularity/
    IModuleConfigProvider<T>          ← interface pure, aucune dépendance ASP.NET

Granit.Core.Endpoints/
    ModuleConfigEndpointExtensions    ← MapGranitModuleConfig<TProvider, TResponse>()

{Module}/Endpoints/ ou {Module}.Endpoints/
    {Module}ConfigProvider            ← implémentation spécifique
```

L'interface vit dans `Granit.Core.Modularity` car c'est une capacité du module,
pas du endpoint. Un module sans endpoints (worker, job) peut implémenter
`IModuleConfigProvider<T>` et exposer sa config via un autre canal.

## Implémentation

### 1 — Créer le DTO de réponse

```csharp
namespace Granit.Webhooks.Dtos;

public sealed record WebhookModuleConfigResponse(bool StorePayload);
```

### 2 — Créer le provider

```csharp
using Granit.Core.Modularity;

namespace Granit.Webhooks.Endpoints;

internal sealed class WebhookModuleConfigProvider(IOptions<WebhooksOptions> options)
    : IModuleConfigProvider<WebhookModuleConfigResponse>
{
    public WebhookModuleConfigResponse GetConfig() =>
        new(options.Value.StorePayload);
}
```

Le provider est `internal` — seul le module le connaît. Il est résolu par DI
dans le endpoint.

### 3 — Enregistrer en DI

Dans la méthode `AddGranit*()` du module :

```csharp
builder.Services.AddScoped<WebhookModuleConfigProvider>();
```

### 4 — Mapper le endpoint

```csharp
public static IEndpointRouteBuilder MapGranitWebhooksConfig(
    this IEndpointRouteBuilder endpoints,
    string routePrefix = "webhooks") =>
    endpoints.MapGranitModuleConfig<WebhookModuleConfigProvider, WebhookModuleConfigResponse>(
        routePrefix, "GetWebhooksConfig", "Webhooks");
```

Résultat : `GET /webhooks/config` retourne `{ "storePayload": true }`.

## Personnalisation du endpoint

`MapGranitModuleConfig` accepte un `Action<RouteHandlerBuilder>?` optionnel
pour personnaliser le endpoint :

```csharp
endpoints.MapGranitModuleConfig<MyProvider, MyResponse>(
    "mymodule", "GetMyModuleConfig", "MyModule",
    route => route
        .AllowAnonymous()
        .RequireRateLimiting("fixed"));
```

Cas d'usage courants :

| Personnalisation | Code |
| --- | --- |
| Endpoint anonyme | `.AllowAnonymous()` |
| Rate limiting | `.RequireRateLimiting("policy")` |
| Autorisation spécifique | `.RequireAuthorization("AdminPolicy")` |

## Prérequis DI

`MapGranitModuleConfig` résout le `TProvider` depuis le conteneur DI. Le provider
**doit** être enregistré avant `app.Build()` — typiquement dans la méthode
`AddGranit*()` du module.

```csharp
// ✅ Correct — enregistré dans AddGranitWebhooks(), avant Build()
builder.Services.AddScoped<WebhookModuleConfigProvider>();

// ❌ Impossible — après Build(), IServiceCollection n'est plus accessible
app.MapGranitModuleConfig<...>(); // le provider doit déjà être dans le DI
```

Si le module n'a pas de phase `AddGranit*()` dédiée (ex. Cookies.Endpoints),
ne pas utiliser `MapGranitModuleConfig` — résoudre les dépendances directement
dans le delegate `MapGet` et instancier le provider inline.

## Provider avec plusieurs dépendances

Le provider peut agréger autant de services que nécessaire :

```csharp
internal sealed class CookieConsentConfigProvider(
    ICookieRegistry cookieRegistry,
    IThirdPartyServiceRegistry serviceRegistry)
    : IModuleConfigProvider<CookieConsentConfigResponse>
{
    public CookieConsentConfigResponse GetConfig()
    {
        var cookies = cookieRegistry.GetAll()
            .Select(c => new CookieDefinitionResponse(c.Name, ...))
            .ToList();

        var services = serviceRegistry.GetAll()
            .Select(s => new ThirdPartyServiceResponse(s.Name, ...))
            .ToList();

        return new CookieConsentConfigResponse(cookies, services);
    }
}
```

> **Note** : `CookieConsentConfigProvider` implémente `IModuleConfigProvider<T>`
> mais n'est pas résolu via `MapGranitModuleConfig`. Il est instancié inline dans
> `MapGranitCookieConsent` car `Granit.Cookies.Endpoints` n'a pas de phase DI
> `AddGranit*()` dédiée. Le provider reste utile pour le futur endpoint admin agrégé.

## Implémentations existantes

| Module | Provider | Endpoint | Via `MapGranitModuleConfig` |
| --- | --- | --- | --- |
| Webhooks | `WebhookModuleConfigProvider` | `GET /webhooks/config` | Oui (DI dans `AddGranitWebhooks`) |
| Cookies | `CookieConsentConfigProvider` | `GET /cookies/config` | Non (instancié inline) |

## Conventions

- **Nommage** : `{Module}ConfigProvider` ou `{Module}ModuleConfigProvider`
- **Visibilité** : `internal sealed` — seul le module le connaît
- **DTO** : `sealed record` suffixé `*Response` (ex. `WebhookModuleConfigResponse`)
- **Namespace** : `{Module}.Endpoints` ou `{Module}` si pas de package Endpoints séparé
- **DI** : enregistré dans le `AddGranit*()` du module quand `MapGranitModuleConfig` est utilisé

## Signature de l'extension

```csharp
public static IEndpointRouteBuilder MapGranitModuleConfig<TProvider, TResponse>(
    this IEndpointRouteBuilder endpoints,
    string routePrefix,
    string endpointName,
    string tag,
    Action<RouteHandlerBuilder>? configureEndpoint = null)
    where TProvider : class, IModuleConfigProvider<TResponse>
    where TResponse : class
```

| Paramètre | Description | Exemple |
| --- | --- | --- |
| `routePrefix` | Préfixe de route du module | `"webhooks"`, `"cookies"` |
| `endpointName` | Nom unique pour link generation | `"GetWebhooksConfig"` |
| `tag` | Tag OpenAPI | `"Webhooks"` |
| `configureEndpoint` | Personnalisation du `RouteHandlerBuilder` | `.AllowAnonymous()` |

## Dépendances

| Composant | Package | Dépendance ASP.NET |
| --- | --- | --- |
| `IModuleConfigProvider<T>` | `Granit.Core` (`Modularity/`) | Non |
| `MapGranitModuleConfig` | `Granit.Core` (`Endpoints/`) | Oui (`FrameworkReference`) |
