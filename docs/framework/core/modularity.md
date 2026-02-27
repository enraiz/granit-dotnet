# Modularity

`Granit.Core` fournit un système de modules inspiré de
[Volo.Abp.Modularity](https://abp.io/docs/latest/framework/architecture/modularity/basics).
Chaque package Granit déclare un **module** qui s'enregistre automatiquement dans le
conteneur DI. Les dépendances entre modules sont résolues par tri topologique et chargées
dans le bon ordre.

L'objectif : **un seul appel** dans `Program.cs` remplace tous les `AddGranit*()` individuels.

```csharp
// Avant (6 appels manuels, ordre à respecter)
builder.AddGranitObservability();
builder.Services.AddGranitSecurity();
builder.Services.AddGranitTiming();
builder.Services.AddGranitGuids();
builder.Services.AddGranitPersistence();
builder.Services.AddGranitVault();

// Après (single entry point, async recommandé)
await builder.AddGranitAsync<GuavaHostModule>();
```

## Installation

```bash
dotnet add package Granit.Core
```

Ce package est automatiquement tiré comme dépendance transitive par tous les packages
Granit. Il n'est nécessaire de le référencer explicitement que dans le projet Host
(composition root) et dans les projets qui utilisent les types domaine (`AuditedEntity`,
`FullAuditedEntity`, `ISoftDeletable`).

## Concepts

### Module = package auto-contenu

Chaque package Granit est un **module** au sens ABP : il contient à la fois ses
**interfaces** (contrats) et ses **implémentations**. Il n'y a pas de package
`Abstractions` centralisé. Les interfaces vivent dans le même package que leur
implémentation :

| Package | Interface | Implémentation |
| --- | --- | --- |
| Granit.Timing | `IClock`, `ICurrentTimezoneProvider` | `Clock`, `CurrentTimezoneProvider` |
| Granit.Guids | `IGuidGenerator` | `SequentialGuidGenerator`, `SimpleGuidGenerator` |
| Granit.Security | `ICurrentUserService` | `KeycloakCurrentUserService` |
| Granit.Vault | `ITransitEncryptionService` | `VaultTransitEncryptionService` |

Les types domaine partagés (`Entity`, `CreationAuditedEntity`, `AuditedEntity`,
`FullAuditedEntity`, `ISoftDeletable`, `AuditLogEntry`) vivent dans
`Granit.Core.Domain` car ils n'ont pas d'implémentation associée.

### Lifecycle (sync + async)

Le système de modules utilise un lifecycle à deux phases, chacune avec une variante
synchrone et asynchrone :

```text
1. ConfigureServices / ConfigureServicesAsync       ─── enregistrement DI
2. OnApplicationInitialization / OnApplicationInitializationAsync ─── après Build(), avant Run()
```

Les variantes async appellent par défaut la version sync (pattern identique à ABP).
Un module peut surcharger **l'une ou l'autre** selon ses besoins :

- **Sync** : suffisant pour la majorité des modules (enregistrement DI classique)
- **Async** : pour les modules nécessitant une initialisation asynchrone (lecture de
  secrets distants, vérification de connectivité, etc.)

`AddGranitAsync<T>()` appelle `ConfigureServicesAsync()` sur chaque module, ce qui
par défaut délègue à `ConfigureServices()`. Ainsi, les modules existants avec uniquement
des overrides sync fonctionnent sans modification.

Ce design est extensible. Les méthodes suivantes peuvent être ajoutées sans breaking
change :

- `PreConfigureServices` / `PostConfigureServices`
- `OnPreApplicationInitialization` / `OnPostApplicationInitialization`
- `OnApplicationShutdown` / `OnApplicationShutdownAsync`

## Classe de module

Tout module Granit hérite de `GranitModule` et surcharge les méthodes lifecycle
nécessaires :

```csharp
// Module sync (cas standard)
public sealed class GranitTimingModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddGranitTiming();
    }
}

// Module async (pour les initialisations asynchrones)
public sealed class MyRemoteConfigModule : GranitModule
{
    public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        var remoteConfig = await FetchRemoteConfigAsync();
        context.Services.Configure<MyOptions>(o => o.Value = remoteConfig);
    }
}
```

La classe de base fournit des implémentations vides (no-op) pour toutes les méthodes.
Les variantes async appellent par défaut la version sync. Un module qui n'a besoin que de
déclarer des dépendances sans logique propre peut hériter sans surcharger quoi que ce soit.

### ServiceConfigurationContext

Le contexte passé à `ConfigureServices` expose :

| Propriété | Type | Description |
| --- | --- | --- |
| `Services` | `IServiceCollection` | Collection de services pour l'enregistrement DI |
| `Configuration` | `IConfiguration` | Configuration de l'application (appsettings, variables d'environnement) |
| `Builder` | `IHostApplicationBuilder` | Builder complet, nécessaire pour certains modules (ex: Observability utilise `builder.Host.UseSerilog()`) |
| `Items` | `IDictionary<string, object?>` | Dictionnaire d'état partagé pour la communication inter-modules pendant ConfigureServices |

Exemple avec accès à la configuration et au builder :

```csharp
public sealed class GranitSecurityModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddGranitSecurity();
    }
}

public sealed class GranitObservabilityModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Builder.AddGranitObservability();
    }
}
```

### ApplicationInitializationContext

Le contexte passé à `OnApplicationInitialization` expose le `IServiceProvider` résolu
après `Build()`. Utilisé pour les initialisations qui nécessitent des services déjà
construits :

```csharp
public override void OnApplicationInitialization(ApplicationInitializationContext context)
{
    var logger = context.ServiceProvider.GetRequiredService<ILogger<MyModule>>();
    logger.LogInformation("Module initialisé");
}
```

## Dépendances entre modules

### DependsOn

L'attribut `[DependsOn]` déclare les dépendances d'un module. Le système garantit que
les modules dépendants sont chargés **avant** le module qui en dépend :

```csharp
[DependsOn(typeof(GranitTimingModule))]
[DependsOn(typeof(GranitGuidsModule))]
[DependsOn(typeof(GranitSecurityModule))]
public sealed class GranitPersistenceModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddGranitPersistence();
    }
}
```

Syntaxe alternative avec un seul attribut et plusieurs types :

```csharp
[DependsOn(
    typeof(GranitTimingModule),
    typeof(GranitGuidsModule),
    typeof(GranitSecurityModule))]
public sealed class GranitPersistenceModule : GranitModule { ... }
```

Les deux syntaxes sont équivalentes. `AllowMultiple = true` et `params Type[]` sont
supportés.

### Résolution transitive

Les dépendances sont résolues **transitivement**. Déclarer une dépendance sur
`GranitPersistenceModule` tire automatiquement `Timing`, `Guids` et `Security` :

```csharp
// Persistence tire automatiquement Timing + Guids + Security
[DependsOn(typeof(GranitPersistenceModule))]
public sealed class MyModule : GranitModule { ... }
```

### Déduplication

Si un module apparaît plusieurs fois dans le graphe de dépendances (cas du diamant),
il n'est chargé qu'**une seule fois** :

```text
GuavaHostModule
├── GranitPersistenceModule
│   ├── GranitTimingModule      ← chargé une fois
│   ├── GranitGuidsModule
│   └── GranitSecurityModule    ← chargé une fois
└── GranitSecurityModule        ← déjà chargé, ignoré
```

### Détection de cycles

Le chargeur détecte les dépendances circulaires et lance une `InvalidOperationException`
avec la liste des modules impliqués :

```text
InvalidOperationException: Dependance circulaire detectee entre les modules : ModuleA, ModuleB.
```

## Graphe de dépendances Granit

```text
GranitTimingModule        ── (standalone → Core)
GranitGuidsModule         ── (standalone → Core)
GranitSecurityModule      ── (standalone → Core)
GranitObservabilityModule ── (standalone → Core)
GranitVaultModule         ── (standalone → Core)
GranitMultiTenancyModule  ── (standalone → Core)   ← dépendance optionnelle

GranitPersistenceModule   ── → Timing, Guids, Security
GranitSettingsModule      ── → Security
GranitWolverineModule     ── → Security
GranitAuthorizationModule ── → Security
GranitIdempotencyModule   ── → (standalone → Core)

GuavaHostModule (application) ── → Observability, Security, Persistence, Vault, MultiTenancy
```

> **Dépendance souple sur `ICurrentTenant`** : `Granit.Persistence`, `Granit.Settings`,
> `Granit.Wolverine`, `Granit.Authorization` et `Granit.Idempotency` consomment
> `ICurrentTenant` (namespace `Granit.Core.MultiTenancy`) sans déclarer de `[DependsOn]`
> vers `GranitMultiTenancyModule`. Un `NullTenantContext` est enregistré par défaut par
> `AddGranit<T>()`. Si `GranitMultiTenancyModule` est dans le graphe, il remplace cet
> enregistrement par l'implémentation réelle. Voir
> [core.md — Dépendance optionnelle sur le multi-tenancy](core.md#dépendance-optionnelle-sur-le-multi-tenancy).

Ordre de chargement résolu pour `GuavaHostModule` (tri topologique) :

```text
1. GranitTimingModule
2. GranitGuidsModule
3. GranitSecurityModule
4. GranitObservabilityModule
5. GranitVaultModule
6. GranitMultiTenancyModule
7. GranitPersistenceModule    (après Timing, Guids, Security)
8. GuavaHostModule            (après tous les autres)
```

## Point d'entrée : AddGranitAsync / UseGranitAsync

### AddGranitAsync (recommandé)

`AddGranitAsync<TModule>()` est le point d'entrée recommandé dans `Program.cs`. Il :

1. Découvre récursivement tous les modules via `[DependsOn]`
2. Les trie par ordre topologique (algorithme de Kahn)
3. Appelle `ConfigureServicesAsync()` sur chaque module dans l'ordre
4. Enregistre `GranitApplication` comme singleton dans le conteneur

```csharp
var builder = WebApplication.CreateBuilder(args);
await builder.AddGranitAsync<GuavaHostModule>();
```

Une variante synchrone `AddGranit<TModule>()` existe pour les cas où l'async n'est
pas souhaité. Elle appelle `ConfigureServices()` (sync) au lieu de
`ConfigureServicesAsync()`.

### UseGranitAsync (recommandé)

`UseGranitAsync()` est appelé après `Build()`, avant `Run()`. Il résout le singleton
`GranitApplication` et appelle `OnApplicationInitializationAsync()` sur chaque module :

```csharp
var app = builder.Build();
await app.UseGranitAsync();
app.Run();
```

Les surcharges sync et async sont disponibles selon le type d'hôte :

| Type | Sync | Async |
| --- | --- | --- |
| `WebApplication` | `app.UseGranit()` | `await app.UseGranitAsync()` |
| `IApplicationBuilder` | `app.UseGranit()` | `await app.UseGranitAsync()` |
| `IHost` | `host.UseGranit()` | `await host.UseGranitAsync()` |

## Créer un module applicatif

Un module applicatif (ex : `GuavaHostModule`) suit le même pattern que les modules
Granit :

```csharp
using Asp.Versioning;
using Granit.Core.Modularity;
using Granit.Observability;
using Granit.Persistence;
using Granit.Security;
using Granit.Vault;

namespace Guava.Host;

[DependsOn(typeof(GranitObservabilityModule))]
[DependsOn(typeof(GranitSecurityModule))]
[DependsOn(typeof(GranitPersistenceModule))]
[DependsOn(typeof(GranitVaultModule))]
public sealed class GuavaHostModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Services spécifiques au Host
        context.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        context.Services.AddHealthChecks();
    }
}
```

Le `Program.cs` résultant est minimal :

```csharp
using Granit.Core.Extensions;
using Guava.Host;
using Guava.Modules.Auth;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// --- Granit (single entry point) ---
await builder.AddGranitAsync<GuavaHostModule>();

// --- Modules applicatifs ---
AuthModule.ConfigureServices(builder.Services, builder.Configuration);

// --- Wolverine (CQRS + HTTP endpoints) ---
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(AuthModule).Assembly);
    opts.UseFluentValidation();
});

var app = builder.Build();

await app.UseGranitAsync();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/healthz");
app.MapWolverineEndpoints(opts =>
{
    opts.UseFluentValidationProblemDetailMiddleware();
});

app.Run();
```

## Architecture interne

### Cycle de vie du chargement

```mermaid
sequenceDiagram
    participant P as Program.cs
    participant GA as GranitApplication
    participant ML as ModuleLoader
    participant A as Module A
    participant B as Module B (depends on A)

    P->>GA: AddGranitAsync&lt;B&gt;()
    GA->>ML: LoadModules&lt;B&gt;()
    ML->>ML: Récursion via [DependsOn]
    ML->>ML: Déduplication par Type
    ML->>ML: TopologicalSort (Kahn)
    ML-->>GA: [A, B]

    GA->>A: ConfigureServicesAsync(context)
    A-->>GA: Services enregistrés
    GA->>B: ConfigureServicesAsync(context)
    B-->>GA: Services enregistrés

    Note over P,B: Phase 1 terminée — Build()

    P->>GA: UseGranitAsync()
    GA->>A: OnApplicationInitializationAsync(context)
    GA->>B: OnApplicationInitializationAsync(context)

    Note over P,B: Phase 2 terminée — Run()
```

### Algorithme de chargement

Le `ModuleLoader` utilise l'algorithme de Kahn pour le tri topologique :

```text
1. DiscoverModules(rootType)
   ├── Récursion via [DependsOn] sur chaque type
   ├── Déduplication par Type (Dictionary<Type, ModuleDescriptor>)
   ├── Validation : chaque type doit hériter de GranitModule
   └── Instanciation : Activator.CreateInstance() (constructeur sans paramètre)

2. TopologicalSort(descriptors)
   ├── Calcul du degré entrant (in-degree) de chaque noeud
   ├── File (queue) des noeuds sans dépendance entrante
   ├── BFS : retrait progressif des arêtes
   └── Détection de cycle : si sorted.Count != total → InvalidOperationException
```

### Classes internes

| Classe | Visibilité | Rôle |
| --- | --- | --- |
| `ModuleLoader` | `internal` | Découverte + tri topologique |
| `ModuleDescriptor` | `internal` | Associe Type + Instance + Dependencies |
| `GranitApplication` | `public` | Singleton, orchestre le lifecycle |

`GranitApplication` est le seul type public du moteur. Ses méthodes (`ConfigureServices`,
`ConfigureServicesAsync`, `InitializeApplication`, `InitializeApplicationAsync`) sont
`internal` pour que seules les extensions `AddGranit`/`AddGranitAsync` et
`UseGranit`/`UseGranitAsync` puissent les appeler.

### Structure des fichiers

```text
Granit.Core
├── Domain/
│   ├── Entity.cs                   (classe de base, identifiant)
│   ├── CreationAuditedEntity.cs    (+ CreatedAt, CreatedBy)
│   ├── AuditedEntity.cs            (+ ModifiedAt, ModifiedBy)
│   ├── FullAuditedEntity.cs        (+ ISoftDeletable)
│   ├── ISoftDeletable.cs
│   └── AuditLogEntry.cs
├── MultiTenancy/
│   ├── ICurrentTenant.cs           (interface — disponible sans Granit.MultiTenancy)
│   └── NullTenantContext.cs        (Null Object, enregistré par défaut)
├── Modularity/
│   ├── GranitModule.cs         (classe de base)
│   ├── DependsOnAttribute.cs       (déclaration de dépendances)
│   ├── ServiceConfigurationContext.cs
│   ├── ApplicationInitializationContext.cs
│   ├── ModuleDescriptor.cs         (internal)
│   ├── ModuleLoader.cs             (internal, tri topologique)
│   └── GranitApplication.cs    (singleton, lifecycle)
└── Extensions/
    ├── GranitHostBuilderExtensions.cs   (AddGranit<T> / AddGranitAsync<T>)
    └── GranitApplicationExtensions.cs   (UseGranit / UseGranitAsync)
```

## Tests

Le projet `Granit.Core.Tests` couvre trois aspects :

### ModuleLoaderTests

Vérifie le tri topologique et la gestion d'erreurs :

```csharp
[Fact]
public void LoadModules_LinearChain_ReturnsDependenciesFirst()
{
    // C → B → A : ordre attendu A, B, C
    var modules = ModuleLoader.LoadModules<ModuleC>();
    var types = modules.Select(m => m.ModuleType).ToList();

    types.IndexOf(typeof(ModuleA)).Should().BeLessThan(types.IndexOf(typeof(ModuleB)));
    types.IndexOf(typeof(ModuleB)).Should().BeLessThan(types.IndexOf(typeof(ModuleC)));
}

[Fact]
public void LoadModules_CircularDependency_ThrowsInvalidOperationException()
{
    var act = () => ModuleLoader.LoadModules<CircularA>();
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*circulaire*");
}
```

Scénarios couverts :

- Module unique sans dépendance
- Chaîne linéaire A → B → C
- Diamant (shared dependency chargée une fois)
- Dépendance circulaire (exception)
- Type non-GranitModule (exception)
- Dépendances dupliquées (déduplication)

### GranitApplicationTests

Vérifie l'ordre d'appel des méthodes lifecycle (sync et async) :

```csharp
[Fact]
public void ConfigureServices_CallsModulesInTopologicalOrder()
{
    var modules = ModuleLoader.LoadModules<TrackingModuleB>();
    var app = new GranitApplication(modules);
    // ...
    app.ConfigureServices(context);
    CallOrder.Should().ContainInOrder("ConfigureServices:A", "ConfigureServices:B");
}

[Fact]
public async Task ConfigureServicesAsync_CallsModulesInTopologicalOrder()
{
    var modules = ModuleLoader.LoadModules<AsyncTrackingModuleB>();
    var app = new GranitApplication(modules);
    // ...
    await app.ConfigureServicesAsync(context);
    CallOrder.Should().ContainInOrder("ConfigureServicesAsync:A", "ConfigureServicesAsync:B");
}
```

Scénarios async supplémentaires :

- Modules avec override async uniquement (vraie async)
- Modules avec override sync appelés via le chemin async (délégation)
- Modules no-op via le chemin async

### IntegrationTests

Vérifie le pipeline complet sync et async :

```csharp
[Fact]
public async Task AddGranitAsync_RegistersGranitApplicationAsSingleton()
{
    var builder = WebApplication.CreateBuilder();
    await builder.AddGranitAsync<AsyncTestRootModule>();
    await using var app = builder.Build();

    var granitApp = app.Services.GetService<GranitApplication>();
    granitApp.Should().NotBeNull();
}
```

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| HDS - Audit trail | Modules chargés dans un ordre déterministe et reproductible |
| HDS - Traçabilité | `GranitApplication.GetModuleTypes()` expose la liste des modules chargés (diagnostics) |
| ISO 9001 - Reproductibilité | Tri topologique = même ordre à chaque démarrage |
| Sécurité - Least privilege | Chaque module n'enregistre que ses propres services |

## Différences avec ABP

| Aspect | ABP | Granit |
| --- | --- | --- |
| Lifecycle | 7 méthodes (Pre/Post + Shutdown) | 2 méthodes (extensible) |
| Async | `AddApplicationAsync` + `*Async` lifecycle | `AddGranitAsync` + `*Async` lifecycle |
| Constructeur | Sans paramètre | Sans paramètre |
| Découverte | Récursive via `[DependsOn]` | Idem |
| Tri | Topologique | Idem (Kahn) |
| DI | `IServiceCollection` via contexte | Idem |
| `Configure<T>()` helper | Oui (raccourci Options) | Non (utiliser `context.Services.Configure<T>()`) |
| Packages Abstractions | Par module (ex: `Volo.Abp.Timing.Abstractions`) | Interface dans le module directement |
| AdditionalAssembly | Oui | Non (YAGNI) |

## Dépendances

| Package | Rôle |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `IServiceCollection` |
| `Microsoft.Extensions.Hosting.Abstractions` | `IHostApplicationBuilder`, `IHost` |
| `Microsoft.Extensions.Options` | `IOptions<T>` |
| `Microsoft.AspNetCore.App` (FrameworkReference) | `IApplicationBuilder`, `WebApplication` |
