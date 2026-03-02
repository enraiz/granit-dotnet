# Architecture (.NET / C#)

[← Conventions](../index.md)

## Structure des projets et packages

- **Un projet = un package NuGet**
- **Namespace = nom du projet** (`Granit.Vault`, `Granit.Persistence`)
- **Zéro référence circulaire**

Organisation type d'un package :

```text
src/Granit.Vault/
├── Extensions/
│   └── VaultServiceCollectionExtensions.cs
├── Options/
│   └── VaultOptions.cs
├── Services/
│   └── TransitEncryptionService.cs
├── GranitVaultModule.cs
└── README.md
```

Chaque package possède son projet de tests miroir (`tests/Granit.Vault.Tests/`).

## Modèle domaine

Granit fournit une hiérarchie d'entités avec audit trail intégré, conforme aux
exigences HDS. Choisissez le niveau d'audit adapté au besoin réglementaire.

| Classe | Champs ajoutés | Usage |
| --- | --- | --- |
| `Entity` | `Id` (Guid) | Entité simple sans audit |
| `CreationAuditedEntity` | `CreatedAt`, `CreatedBy` | Traçabilité de la création |
| `AuditedEntity` | `ModifiedAt`, `ModifiedBy` | Traçabilité création + modification |
| `FullAuditedEntity` | `IsDeleted`, `DeletedAt`, `DeletedBy` | Soft delete RGPD |

Conventions :

- Propriétés `string` non-nullable initialisées à `string.Empty`
- `DateTimeOffset` pour tous les horodatages (jamais `DateTime`)
- Les champs d'audit sont remplis automatiquement par les intercepteurs Granit

> Voir aussi : [tutoriel domaine](../demarrage-rapide/03-domaine.md),
> [référence domaine](../../framework/data/domain.md)

## Constructeurs et injection de dépendances

### Constructeurs primaires (C# 12+)

Deux patterns acceptés :

```csharp
// Pattern 1 — paramètre direct (préféré pour les services simples)
public sealed class MyService(ILogger<MyService> logger, IClock clock) : IMyService
{
    public void DoWork() => logger.LogInformation("...");
}

// Pattern 2 — champs readonly (quand l'immutabilité explicite est critique)
public sealed class AuditedEntityInterceptor(
    ICurrentUserService currentUserService,
    IClock clock) : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IClock _clock = clock;
}
```

Les paramètres de constructeur primaire sont mutables par capture. Le pattern 2
garantit l'immutabilité via `readonly`. Choisissez selon le contexte.

### Enregistrement DI

- **Récepteur `IServiceCollection`** (par défaut) pour les packages composables
- **Récepteur `IHostApplicationBuilder`** uniquement quand le package nécessite
  `Configuration` **et** appelle `ValidateOnStart()`

Pattern options moderne :

```csharp
services.AddOptions<VaultOptions>()
    .BindConfiguration(VaultOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Durées de vie

| Durée | Quand l'utiliser | Exemple |
| --- | --- | --- |
| `Singleton` | Service stateless, `TimeProvider` | `TryAddSingleton<IClock, Clock>()` |
| `Scoped` | Service lié à la requête HTTP | `AddScoped<AuditedEntityInterceptor>()` |
| `Transient` | Handler léger sans état | `AddTransient<INotificationHandler>()` |

### Bonnes pratiques

- `TryAdd*` pour éviter les doubles enregistrements
- Retour fluent (`return services`) pour le chaînage

> Voir aussi : [tutoriel persistance](../demarrage-rapide/04-persistance.md)

## Endpoints et API

- **Minimal API uniquement** — pas de contrôleurs MVC
- Pattern Granit en deux niveaux :
  1. Extension sur `IEndpointRouteBuilder` (point d'entrée public, appelé
     dans `Program.cs`)
  2. Extension sur `RouteGroupBuilder` (organisation interne des routes)
- **Nommage** : `{Entity}EndpointRouteBuilderExtensions` +
  `{Entity}Endpoints`
- **Versioning** via `Asp.Versioning` (`/api/v{version}/...`)
- **Réponses HTTP** standardisées (RFC 7807 pour les erreurs)

```csharp
public static class TaskEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTaskEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        group.MapTaskRoutes();
        return group;
    }
}
```

> Voir aussi : [tutoriel endpoints](../demarrage-rapide/05-endpoints.md),
> [versioning API](../../framework/api/api-versioning.md),
> [réponses HTTP](../../framework/api/http-responses.md),
> [idempotence](../../framework/api/idempotency.md),
> [documentation OpenAPI](../../framework/api/api-documentation.md)

## Appels HTTP sortants (HttpClient)

### Règle absolue

**Jamais de `new HttpClient()`** — utilisez toujours `IHttpClientFactory` pour
bénéficier du pooling de connexions, de la gestion du DNS et de la résilience.

### Typed Client (pattern recommandé)

```csharp
/// <summary>
/// Typed HTTP client for the geocoding API.
/// </summary>
public sealed class GeoService(HttpClient httpClient)
{
    public async Task<GeoResult?> GeocodeAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        return await httpClient
            .GetFromJsonAsync<GeoResult>($"/geocode?q={Uri.EscapeDataString(address)}", cancellationToken)
            .ConfigureAwait(false);
    }
}
```

### Enregistrement avec résilience

```csharp
services.AddHttpClient<GeoService>(client =>
    {
        client.BaseAddress = new Uri("https://api.geo.example.com");
    })
    .AddStandardResilienceHandler(); // .NET 8+ (Microsoft.Extensions.Http.Resilience)
```

`AddStandardResilienceHandler()` ajoute automatiquement :

- **Retry** (3 tentatives, backoff exponentiel)
- **Circuit breaker** (coupe après 10 % d'échecs sur 30 s)
- **Timeout** par requête (30 s) et total (2 min)
- **Rate limiter** (concurrence)

Pour personnaliser, passez une `Action<HttpStandardResilienceOptions>`.

### Anti-patterns

| Anti-pattern | Problème | Solution |
| --- | --- | --- |
| `new HttpClient()` | Socket exhaustion, DNS stale | `IHttpClientFactory` |
| `HttpClient` en singleton | DNS stale après TTL | `IHttpClientFactory` gère le pooling |
| `StringContent` + sérialisation manuelle | Verbeux, erreurs d'encoding | `JsonContent.Create()` ou `PostAsJsonAsync()` |

## Performance EF Core (requêtes)

EF Core est la source n°1 de lenteurs en production. Chaque requête mérite
attention.

### `AsNoTracking` par défaut pour les lectures

```csharp
// ✅ Lecture seule — pas besoin de change tracking
List<Task> tasks = await dbContext.Tasks
    .AsNoTracking()
    .Where(t => t.Status == TaskStatus.Active)
    .ToListAsync(cancellationToken);

// ❌ Change tracking inutile pour une lecture
List<Task> tasks = await dbContext.Tasks
    .Where(t => t.Status == TaskStatus.Active)
    .ToListAsync(cancellationToken);
```

### Projection `Select` — ne charger que le nécessaire

```csharp
// ✅ Ne charge que 3 colonnes
List<TaskSummaryDto> summaries = await dbContext.Tasks
    .AsNoTracking()
    .Where(t => t.Status == TaskStatus.Active)
    .Select(t => new TaskSummaryDto(t.Id, t.Title, t.DueDate))
    .ToListAsync(cancellationToken);

// ❌ Charge toute l'entité pour n'utiliser que 3 champs
List<Task> tasks = await dbContext.Tasks
    .Where(t => t.Status == TaskStatus.Active)
    .ToListAsync(cancellationToken);
```

### Éviter le N+1 — `Include` / `ThenInclude`

```csharp
// ✅ Charge les relations en une seule requête
List<Task> tasks = await dbContext.Tasks
    .Include(t => t.Assignee)
    .Include(t => t.Comments)
        .ThenInclude(c => c.Author)
    .Where(t => t.ProjectId == projectId)
    .ToListAsync(cancellationToken);
```

### Pagination côté serveur

```csharp
// ✅ Pagination avec Skip/Take (toujours OrderBy avant)
List<Task> page = await dbContext.Tasks
    .AsNoTracking()
    .OrderBy(t => t.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

### Règles complémentaires

- **`IQueryable<T>` ne sort jamais de la couche Persistence** — matérialisez
  avec `ToListAsync()` ou projetez en DTO
- **`AsSplitQuery()`** pour les requêtes avec plusieurs `Include` de collections
  (évite l'explosion cartésienne)
- **`ExecuteUpdateAsync()` / `ExecuteDeleteAsync()`** pour les mises à jour en masse
  (pas de chargement des entités en mémoire)
- **Index** : vérifiez que les colonnes filtrées (`Where`) et triées (`OrderBy`)
  ont un index approprié dans la migration
