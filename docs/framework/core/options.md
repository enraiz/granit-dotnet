# Options

`Granit` utilise le **pattern Options** de Microsoft.Extensions.Options,
qui est la manière standard de configurer et consommer des paramètres typés dans ASP.NET Core.
Les packages Granit s'appuient entièrement sur cette infrastructure — aucune abstraction
supplémentaire n'est introduite.

> **Référence Microsoft** :
> [Options pattern dans ASP.NET Core](https://learn.microsoft.com/fr-fr/aspnet/core/fundamentals/configuration/options)
>
> **Voir aussi** : [configuration.md](configuration.md) pour les sources de configuration
> (appsettings.json, variables d'environnement, Vault).

## Principe

Le pattern Options consiste à :

1. Déclarer une classe POCO (Plain Old CLR Object) représentant un groupe de paramètres
2. Lier cette classe à une section de la configuration au démarrage (`Configure<T>`)
3. Injecter `IOptions<T>` dans les services qui ont besoin de ces paramètres

```text
appsettings.json              classe d'options             service consommateur
───────────────               ─────────────────            ────────────────────
"Keycloak": {          →      KeycloakOptions      →       IOptions<KeycloakOptions>
  "Authority": "...",           Authority                   .Value.Authority
  "ClientId": "..."             ClientId                    .Value.ClientId
}
```

## Déclarer une classe d'options

### Convention Granit

Chaque classe d'options Granit suit cette convention :

```csharp
namespace Granit.Authentication.Keycloak.Options;

public sealed class KeycloakOptions
{
    // 1. Constante SectionName — nom de la section dans appsettings.json
    public const string SectionName = "Keycloak";

    // 2. Valeurs par défaut raisonnables sur chaque propriété
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;  // true = valeur sûre
    public string AdminRole { get; set; } = "admin";
}
```

**Règles appliquées dans Granit** :

- `sealed` — pas de dérivation (les options sont des DTOs)
- `SectionName` constant — convention de nommage, évite les chaînes magiques
- Valeurs par défaut — chaque option a une valeur utilisable sans configuration explicite
- Setters publics — requis pour le binding par réflexion

## Enregistrer des options

### Binding depuis la configuration (recommandé)

Toutes les méthodes `AddGranit*()` sont **sans paramètre**. La configuration est résolue
automatiquement depuis le conteneur DI (`IConfiguration` enregistré par le host).
Chaque méthode utilise le pattern standard `BindConfiguration` + validation :

```csharp
// Dans JwtBearerServiceCollectionExtensions.cs
public static IServiceCollection AddGranitJwtBearer(this IServiceCollection services)
{
    services.AddOptions<JwtBearerAuthOptions>()
        .BindConfiguration(JwtBearerAuthOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // ...
    return services;
}
```

L'application hôte fournit uniquement la configuration dans `appsettings.json` :

```json
{
  "Authentication": {
    "Authority": "https://idp.example.com/realms/my-realm",
    "Audience": "my-client",
    "RequireHttpsMetadata": true
  }
}
```

### Binding via appsettings.json (tous les modules)

Toutes les options Granit sont désormais configurées via `appsettings.json`, y compris
celles qui étaient auparavant passées par callback. Cela uniformise la configuration
et permet la surcharge par environnement :

```json
{
  "Clock": {
    "DefaultTimezone": "Europe/Brussels"
  }
}
```

```csharp
// Sans paramètre — les options sont liées via BindConfiguration
builder.Services.AddGranitTiming();
```

### Binding mixte : callback + configuration

Les deux approches peuvent être combinées — la configuration `appsettings.json` fournit
les valeurs de base, le callback permet de les surcharger par code :

```csharp
services
    .AddOptions<MyOptions>()
    .BindConfiguration(MyOptions.SectionName)
    .Configure(options =>
    {
        // Surcharge par code (prioritaire sur appsettings.json)
        options.SomeProperty = ComputedValue();
    });
```

## Consommer des options

### Dans un service (injection par constructeur)

```csharp
public class KeycloakClaimsTransformation : IClaimsTransformation
{
    private readonly KeycloakOptions _options;

    public KeycloakClaimsTransformation(IOptions<KeycloakOptions> options)
    {
        _options = options.Value;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var source = _options.RoleClaimsSource; // "realm_access" ou "resource_access"
        // ...
    }
}
```

### Dans un module Granit

Pendant la phase `ConfigureServices`, les options ne sont pas encore disponibles via
`IOptions<T>` (le conteneur DI n'est pas encore construit). Les méthodes `AddGranit*()`
étant sans paramètre, elles utilisent `BindConfiguration` qui résout `IConfiguration`
depuis le conteneur au moment de la construction des options :

```csharp
public class GranitAuthenticationKeycloakModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Appels sans paramètre — BindConfiguration résout IConfiguration depuis le DI
        context.Services.AddGranitJwtBearer();
        context.Services.AddGranitKeycloak();
    }
}
```

### Dans les tests

`Options.Create()` permet d'instancier `IOptions<T>` sans conteneur DI :

```csharp
// Test unitaire — pas de conteneur DI nécessaire
var options = Options.Create(new KeycloakOptions
{
    Authority = "https://keycloak.test/realms/test",
    ClientId = "test-client",
    RoleClaimsSource = "realm_access"
});

var transformation = new KeycloakClaimsTransformation(options);
```

## IOptions\<T\> vs IOptionsSnapshot\<T\> vs IOptionsMonitor\<T\>

| Interface | Lifetime | Rechargement | Usage Granit |
| --- | --- | --- | --- |
| `IOptions<T>` | Singleton | Non | Standard — valeurs fixes au démarrage |
| `IOptionsSnapshot<T>` | Scoped | Par requête | Non utilisé |
| `IOptionsMonitor<T>` | Singleton | À chaud (fichier) | Non utilisé |

Granit utilise exclusivement `IOptions<T>`. Les options sont lues au démarrage
et restent fixes pendant toute la durée de vie de l'application. Un redémarrage du pod
K8s est nécessaire pour appliquer un changement de configuration — ce comportement
est intentionnel dans une architecture cloud-native.

## PostConfigure — surcharge après binding

`PostConfigure<T>` s'exécute **après** tous les `Configure<T>`, quel que soit leur ordre.
Granit l'utilise dans `Granit.Authentication.Keycloak` pour surcharger les options
JWT Bearer (configurées par `Granit.Authentication.JwtBearer`) avec les valeurs Keycloak :

```csharp
// Dans KeycloakServiceCollectionExtensions.cs
services.PostConfigureAll<JwtBearerOptions>(jwtOptions =>
{
    // Surcharge les valeurs JWT Bearer avec celles de KeycloakOptions
    var keycloak = services.BuildServiceProvider()
        .GetRequiredService<IOptions<KeycloakOptions>>().Value;

    jwtOptions.Authority = keycloak.Authority;
    jwtOptions.Audience = keycloak.Audience ?? keycloak.ClientId;
    jwtOptions.TokenValidationParameters.NameClaimType = "preferred_username";
});
```

Ce mécanisme permet à `Granit.Authentication.Keycloak` de reconfigurer le JWT Bearer
sans que l'application hôte ait à gérer l'ordre d'initialisation manuellement.

```text
Configure<JwtBearerOptions>       (via AddGranitJwtBearer() — section "Authentication")
         ↓
PostConfigureAll<JwtBearerOptions> (via AddGranitKeycloak() — écrase avec Keycloak)
```

## Référence des options Granit

| Classe | Package | Section | Configuré via |
| --- | --- | --- | --- |
| `JwtBearerAuthOptions` | `Granit.Authentication.JwtBearer` | `"Authentication"` | `BindConfiguration` |
| `KeycloakOptions` | `Granit.Authentication.Keycloak` | `"Keycloak"` | `BindConfiguration` |
| `VaultOptions` | `Granit.Vault` | `"Vault"` | `BindConfiguration` |
| `ObservabilityOptions` | `Granit.Observability` | `"Observability"` | `BindConfiguration` |
| `ClockOptions` | `Granit.Timing` | `"Clock"` | `BindConfiguration` |
| `GuidGeneratorOptions` | `Granit.Guids` | `"GuidGenerator"` | `BindConfiguration` |

## Créer des options pour un nouveau module

Exemple complet d'intégration du pattern Options dans un nouveau module :

### 1. Déclarer la classe d'options

```csharp
namespace Granit.MyModule.Options;

public sealed class MyModuleOptions
{
    public const string SectionName = "MyModule";

    public string ApiEndpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableRetry { get; set; } = true;
}
```

### 2. Lier dans la méthode d'extension

```csharp
public static IServiceCollection AddGranitMyModule(this IServiceCollection services)
{
    services.AddOptions<MyModuleOptions>()
        .BindConfiguration(MyModuleOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddScoped<IMyService, MyService>();

    return services;
}
```

### 3. Consommer dans le service

```csharp
public class MyService : IMyService
{
    private readonly MyModuleOptions _options;

    public MyService(IOptions<MyModuleOptions> options)
    {
        _options = options.Value;
    }
}
```

### 4. Configurer dans appsettings.json

```json
{
  "MyModule": {
    "ApiEndpoint": "https://api.example.com",
    "TimeoutSeconds": 60,
    "EnableRetry": true
  }
}
```

### 5. Tester

```csharp
var options = Options.Create(new MyModuleOptions
{
    ApiEndpoint = "https://api.test.com",
    TimeoutSeconds = 5
});

var service = new MyService(options);
```

## Validation des options

Pour détecter les erreurs de configuration au démarrage (fail-fast), ajouter
`ValidateDataAnnotations()` et `ValidateOnStart()` :

```csharp
public sealed class MyModuleOptions
{
    public const string SectionName = "MyModule";

    [Required]
    [Url]
    public string ApiEndpoint { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
```

```csharp
services.AddOptions<MyModuleOptions>()
    .BindConfiguration(MyModuleOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();   // Exception au démarrage si invalide
```

Tous les packages Granit activent `ValidateDataAnnotations()` et `ValidateOnStart()` par
défaut. Les options annotées sont validées au démarrage de l'application (fail-fast).

## Bonnes pratiques

1. **`SectionName` constant** — toujours déclarer `public const string SectionName`
   pour éviter les chaînes magiques et faciliter la recherche dans la codebase
2. **Valeurs par défaut raisonnables** — chaque propriété doit fonctionner sans
   configuration explicite ; les valeurs manquantes critiques doivent être validées
3. **`sealed`** — les classes d'options sont des DTOs, pas des hiérarchies
4. **`IOptions<T>` exclusivement** — ne jamais injecter `IConfiguration` dans un
   service métier ; préférer des classes d'options fortement typées
5. **`Options.Create()` dans les tests** — évite l'instanciation d'un conteneur DI
   complet pour les tests unitaires
6. **`PostConfigure` pour les extensions IDP** — le mécanisme standard pour surcharger
   les options d'un module tiers sans modifier son code
