# Modularity

`Granit.Core` fournit un système de modules inspiré de
[Volo.Abp.Modularity](https://abp.io/docs/latest/framework/architecture/modularity/basics).
Chaque package Foundation déclare un **module** qui s'enregistre automatiquement dans le
conteneur DI. Les dépendances entre modules sont résolues par tri topologique et chargées
dans le bon ordre.

L'objectif : **un seul appel** dans `Program.cs` remplace tous les `AddFoundation*()` individuels.

```csharp
// Avant (6 appels manuels, ordre à respecter)
builder.AddFoundationObservability();
builder.Services.AddFoundationSecurity(builder.Configuration);
builder.Services.AddFoundationTiming();
builder.Services.AddFoundationGuids();
builder.Services.AddFoundationPersistence();
builder.Services.AddFoundationVault(builder.Configuration);

// Après (single entry point, async recommandé)
await builder.AddFoundationAsync<GuavaHostModule>();
```

## Installation

```bash
dotnet add package Granit.Core
```

Ce package est automatiquement tiré comme dépendance transitive par tous les packages
Foundation. Il n'est nécessaire de le référencer explicitement que dans le projet Host
(composition root) et dans les projets qui utilisent les types domaine (`AuditedEntity`,
`FullAuditedEntity`, `ISoftDeletable`).

## Concepts

### Module = package auto-contenu

Chaque package Foundation est un **module** au sens ABP : il contient à la fois ses
**interfaces** (contrats) et ses **implémentations**. Il n'y a pas de package
`Abstractions` centralisé. Les interfaces vivent dans le même package que leur
implémentation :

| Package | Interface | Implémentation |
| --- | --- | --- |
| Foundation.Timing | `IClock`, `ICurrentTimezoneProvider` | `Clock`, `CurrentTimezoneProvider` |
| Foundation.Guids | `IGuidGenerator` | `SequentialGuidGenerator`, `SimpleGuidGenerator` |
| Foundation.Security | `ICurrentUserService` | `KeycloakCurrentUserService` |
| Foundation.Vault | `ITransitEncryptionService` | `VaultTransitEncryptionService` |

Les types domaine partagés (`Entity`, `CreationAuditedEntity`, `AuditedEntity`,
`FullAuditedEntity`, `ISoftDeletable`, `AuditLogEntry`) vivent dans
`Foundation.Core.Domain` car ils n'ont pas d'implémentation associée.

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

`AddFoundationAsync<T>()` appelle `ConfigureServicesAsync()` sur chaque module, ce qui
par défaut délègue à `ConfigureServices()`. Ainsi, les modules existants avec uniquement
des overrides sync fonctionnent sans modification.

Ce design est extensible. Les méthodes suivantes peuvent être ajoutées sans breaking
change :

- `PreConfigureServices` / `PostConfigureServices`
- `OnPreApplicationInitialization` / `OnPostApplicationInitialization`
- `OnApplicationShutdown` / `OnApplicationShutdownAsync`

## Classe de module

Tout module Foundation hérite de `FoundationModule` et surcharge les méthodes lifecycle
nécessaires :

```csharp
// Module sync (cas standard)
public sealed class FoundationTimingModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFoundationTiming();
    }
}

// Module async (pour les initialisations asynchrones)
public sealed class MyRemoteConfigModule : FoundationModule
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
public sealed class FoundationSecurityModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFoundationSecurity(context.Configuration);
    }
}

public sealed class FoundationObservabilityModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Builder.AddFoundationObservability();
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
[DependsOn(typeof(FoundationTimingModule))]
[DependsOn(typeof(FoundationGuidsModule))]
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationPersistenceModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFoundationPersistence();
    }
}
```

Syntaxe alternative avec un seul attribut et plusieurs types :

```csharp
[DependsOn(
    typeof(FoundationTimingModule),
    typeof(FoundationGuidsModule),
    typeof(FoundationSecurityModule))]
public sealed class FoundationPersistenceModule : FoundationModule { ... }
```

Les deux syntaxes sont équivalentes. `AllowMultiple = true` et `params Type[]` sont
supportés.

### Résolution transitive

Les dépendances sont résolues **transitivement**. Déclarer une dépendance sur
`FoundationPersistenceModule` tire automatiquement `Timing`, `Guids` et `Security` :

```csharp
// Persistence tire automatiquement Timing + Guids + Security
[DependsOn(typeof(FoundationPersistenceModule))]
public sealed class MyModule : FoundationModule { ... }
```

### Déduplication

Si un module apparaît plusieurs fois dans le graphe de dépendances (cas du diamant),
il n'est chargé qu'**une seule fois** :

```text
GuavaHostModule
├── FoundationPersistenceModule
│   ├── FoundationTimingModule      ← chargé une fois
│   ├── FoundationGuidsModule
│   └── FoundationSecurityModule    ← chargé une fois
└── FoundationSecurityModule        ← déjà chargé, ignoré
```

### Détection de cycles

Le chargeur détecte les dépendances circulaires et lance une `InvalidOperationException`
avec la liste des modules impliqués :

```text
InvalidOperationException: Dependance circulaire detectee entre les modules : ModuleA, ModuleB.
```

## Graphe de dépendances Foundation

```text
FoundationTimingModule        ── (standalone → Core)
FoundationGuidsModule         ── (standalone → Core)
FoundationSecurityModule      ── (standalone → Core)
FoundationObservabilityModule ── (standalone → Core)
FoundationVaultModule         ── (standalone → Core)

FoundationPersistenceModule   ── → Timing, Guids, Security

GuavaHostModule (application) ── → Observability, Security, Persistence, Vault
```

Ordre de chargement résolu pour `GuavaHostModule` (tri topologique) :

```text
1. FoundationTimingModule
2. FoundationGuidsModule
3. FoundationSecurityModule
4. FoundationObservabilityModule
5. FoundationVaultModule
6. FoundationPersistenceModule    (après Timing, Guids, Security)
7. GuavaHostModule                (après tous les autres)
```

## Point d'entrée : AddFoundationAsync / UseFoundationAsync

### AddFoundationAsync (recommandé)

`AddFoundationAsync<TModule>()` est le point d'entrée recommandé dans `Program.cs`. Il :

1. Découvre récursivement tous les modules via `[DependsOn]`
2. Les trie par ordre topologique (algorithme de Kahn)
3. Appelle `ConfigureServicesAsync()` sur chaque module dans l'ordre
4. Enregistre `FoundationApplication` comme singleton dans le conteneur

```csharp
var builder = WebApplication.CreateBuilder(args);
await builder.AddFoundationAsync<GuavaHostModule>();
```

Une variante synchrone `AddFoundation<TModule>()` existe pour les cas où l'async n'est
pas souhaité. Elle appelle `ConfigureServices()` (sync) au lieu de
`ConfigureServicesAsync()`.

### UseFoundationAsync (recommandé)

`UseFoundationAsync()` est appelé après `Build()`, avant `Run()`. Il résout le singleton
`FoundationApplication` et appelle `OnApplicationInitializationAsync()` sur chaque module :

```csharp
var app = builder.Build();
await app.UseFoundationAsync();
app.Run();
```

Les surcharges sync et async sont disponibles selon le type d'hôte :

| Type | Sync | Async |
| --- | --- | --- |
| `WebApplication` | `app.UseFoundation()` | `await app.UseFoundationAsync()` |
| `IApplicationBuilder` | `app.UseFoundation()` | `await app.UseFoundationAsync()` |
| `IHost` | `host.UseFoundation()` | `await host.UseFoundationAsync()` |

## Créer un module applicatif

Un module applicatif (ex : `GuavaHostModule`) suit le même pattern que les modules
Foundation :

```csharp
using Asp.Versioning;
using Granit.Core.Modularity;
using Granit.Observability;
using Granit.Persistence;
using Granit.Security;
using Granit.Vault;

namespace Guava.Host;

[DependsOn(typeof(FoundationObservabilityModule))]
[DependsOn(typeof(FoundationSecurityModule))]
[DependsOn(typeof(FoundationPersistenceModule))]
[DependsOn(typeof(FoundationVaultModule))]
public sealed class GuavaHostModule : FoundationModule
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

// --- Foundation (single entry point) ---
await builder.AddFoundationAsync<GuavaHostModule>();

// --- Modules applicatifs ---
AuthModule.ConfigureServices(builder.Services, builder.Configuration);

// --- Wolverine (CQRS + HTTP endpoints) ---
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(AuthModule).Assembly);
    opts.UseFluentValidation();
});

var app = builder.Build();

await app.UseFoundationAsync();
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

### Algorithme de chargement

Le `ModuleLoader` utilise l'algorithme de Kahn pour le tri topologique :

```text
1. DiscoverModules(rootType)
   ├── Récursion via [DependsOn] sur chaque type
   ├── Déduplication par Type (Dictionary<Type, ModuleDescriptor>)
   ├── Validation : chaque type doit hériter de FoundationModule
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
| `FoundationApplication` | `public` | Singleton, orchestre le lifecycle |

`FoundationApplication` est le seul type public du moteur. Ses méthodes (`ConfigureServices`,
`ConfigureServicesAsync`, `InitializeApplication`, `InitializeApplicationAsync`) sont
`internal` pour que seules les extensions `AddFoundation`/`AddFoundationAsync` et
`UseFoundation`/`UseFoundationAsync` puissent les appeler.

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
├── Modularity/
│   ├── FoundationModule.cs         (classe de base)
│   ├── DependsOnAttribute.cs       (déclaration de dépendances)
│   ├── ServiceConfigurationContext.cs
│   ├── ApplicationInitializationContext.cs
│   ├── ModuleDescriptor.cs         (internal)
│   ├── ModuleLoader.cs             (internal, tri topologique)
│   └── FoundationApplication.cs    (singleton, lifecycle)
└── Extensions/
    ├── FoundationHostBuilderExtensions.cs   (AddFoundation<T> / AddFoundationAsync<T>)
    └── FoundationApplicationExtensions.cs   (UseFoundation / UseFoundationAsync)
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
- Type non-FoundationModule (exception)
- Dépendances dupliquées (déduplication)

### FoundationApplicationTests

Vérifie l'ordre d'appel des méthodes lifecycle (sync et async) :

```csharp
[Fact]
public void ConfigureServices_CallsModulesInTopologicalOrder()
{
    var modules = ModuleLoader.LoadModules<TrackingModuleB>();
    var app = new FoundationApplication(modules);
    // ...
    app.ConfigureServices(context);
    CallOrder.Should().ContainInOrder("ConfigureServices:A", "ConfigureServices:B");
}

[Fact]
public async Task ConfigureServicesAsync_CallsModulesInTopologicalOrder()
{
    var modules = ModuleLoader.LoadModules<AsyncTrackingModuleB>();
    var app = new FoundationApplication(modules);
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
public async Task AddFoundationAsync_RegistersFoundationApplicationAsSingleton()
{
    var builder = WebApplication.CreateBuilder();
    await builder.AddFoundationAsync<AsyncTestRootModule>();
    await using var app = builder.Build();

    var foundationApp = app.Services.GetService<FoundationApplication>();
    foundationApp.Should().NotBeNull();
}
```

## Conformité

| Exigence | Mécanisme |
| --- | --- |
| HDS - Audit trail | Modules chargés dans un ordre déterministe et reproductible |
| HDS - Traçabilité | `FoundationApplication.GetModuleTypes()` expose la liste des modules chargés (diagnostics) |
| ISO 9001 - Reproductibilité | Tri topologique = même ordre à chaque démarrage |
| Sécurité - Least privilege | Chaque module n'enregistre que ses propres services |

## Différences avec ABP

| Aspect | ABP | Foundation |
| --- | --- | --- |
| Lifecycle | 7 méthodes (Pre/Post + Shutdown) | 2 méthodes (extensible) |
| Async | `AddApplicationAsync` + `*Async` lifecycle | `AddFoundationAsync` + `*Async` lifecycle |
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
