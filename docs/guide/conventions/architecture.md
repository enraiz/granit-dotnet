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
exigences ISO 27001. Choisissez le niveau d'audit adapté au besoin réglementaire.

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

### Pattern REPR (Request-Endpoint-Response)

Granit adopte les **principes** du
[REPR design pattern](https://deviq.com/design-patterns/repr-design-pattern) en les
adaptant aux Minimal API natives de .NET. REPR formalise la séparation entre la requête
entrante, le handler qui la traite, et la réponse retournée — chacun étant un type
dédié. Cette séparation garantit des contrats OpenAPI propres, une testabilité unitaire
aisée et un couplage minimal entre couches.

#### Les trois piliers

| Pilier | Règle Granit | Conséquence |
| --- | --- | --- |
| **Request** | Un `sealed record` dédié par opération | Pas de réutilisation de l'entité EF, pas de `dynamic` |
| **Endpoint** | Une méthode `private static` nommée par route | Pas de contrôleur MVC, pas de lambda inline |
| **Response** | Un `sealed record` distinct de l'entité domaine | Contrat OpenAPI stable, découplé du schéma DB |

#### Ce que Granit fait — et ne fait pas

| Caractéristique | REPR strict (FastEndpoints / Ardalis) | Granit |
| --- | --- | --- |
| Request/Response dédiés par opération | Oui | **Oui** |
| Pas de retour d'entité EF | Oui | **Oui** |
| Un fichier/classe par endpoint | Oui | **Non** — handlers groupés par feature |
| Dépendance externe (FastEndpoints, MediatR) | Oui | **Non** — Minimal API natif |

Granit **groupe les handlers par feature** dans une classe d'extensions sur
`RouteGroupBuilder` au lieu de créer une classe par endpoint. Ce choix pragmatique
réduit le nombre de fichiers tout en préservant la séparation Request / Endpoint /
Response.

#### Structure type d'un package Endpoints

```text
src/Granit.{Module}.Endpoints/
├── Dtos/
│   ├── {Module}{Action}Request.cs      ← Request (input body / query)
│   └── {Module}{Action}Response.cs     ← Response (output)
├── Endpoints/
│   ├── {Module}ReadEndpoints.cs        ← Endpoint (handlers lecture)
│   └── {Module}AdminEndpoints.cs       ← Endpoint (handlers admin)
└── Extensions/
    └── {Module}EndpointRouteBuilderExtensions.cs  ← Point d'entrée public
```

#### Exemple concret (CRUD complet)

```csharp
// --- Dtos/TaskCreateRequest.cs ---
/// <summary>Request to create a new task.</summary>
public sealed record TaskCreateRequest(string Title, string? Description = null);

// --- Dtos/TaskResponse.cs ---
/// <summary>Represents a task returned by the API.</summary>
public sealed record TaskResponse(Guid Id, string Title, string? Description);

// --- Endpoints/TaskEndpoints.cs ---
internal static class TaskEndpoints
{
    internal static void MapTaskRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetTask")
            .WithSummary("Returns a task by its unique identifier.");

        group.MapPost("/", CreateAsync)
            .WithName("CreateTask")
            .WithSummary("Creates a new task.");
    }

    private static async Task<Results<Ok<TaskResponse>, NotFound>> GetByIdAsync(
        Guid id,
        ITaskReader reader,
        CancellationToken cancellationToken)
    {
        TaskResponse? task = await reader.FindAsync(id, cancellationToken)
            .ConfigureAwait(false);
        return task is not null
            ? TypedResults.Ok(task)
            : TypedResults.NotFound();
    }

    private static async Task<Created<TaskResponse>> CreateAsync(
        TaskCreateRequest request,
        ITaskWriter writer,
        CancellationToken cancellationToken)
    {
        TaskResponse created = await writer.CreateAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return TypedResults.Created($"/{created.Id}", created);
    }
}
```

#### Règles de nommage des DTOs

Ces règles sont détaillées dans [style-et-nommage.md](style-et-nommage.md) ; rappel
rapide dans le contexte REPR :

- **Suffixe `Request`** pour les corps de requête (POST/PUT)
- **Suffixe `Response`** pour les retours
- **Jamais `Dto`** comme suffixe
- **Préfixe métier obligatoire** : `WorkflowTransitionRequest`, pas `TransitionRequest`
  (OpenAPI aplatit les namespaces)
- **Types cross-cutting exemptés** : `PagedResult<T>`, `ProblemDetails`

#### Pourquoi pas FastEndpoints / MediatR ?

Granit est un framework open-source minimal : chaque dépendance ajoutée est une
dépendance que les consommateurs héritent. Les Minimal API natives offrent la même
séparation Request/Endpoint/Response sans imposer de couche d'abstraction
supplémentaire. Le regroupement par feature réduit le boilerplate (pas de classe +
héritage + configuration par endpoint) tout en restant facile à tester via des
méthodes statiques pures.

> Voir aussi : [Pattern REPR — catalogue complet](../../patterns/architecture/repr.md)

### Qualité du document OpenAPI

Le document OpenAPI est le **contrat** entre le backend et ses consommateurs (frontend,
clients générés, portail développeur). Chaque endpoint doit produire une sortie OpenAPI
complète et correcte. Les règles suivantes sont **obligatoires**.

#### `TypedResults` obligatoire (pas `Results`)

Les handlers Minimal API doivent retourner des types concrets (`TypedResults.*`) et non
des interfaces (`Results.*`). ASP.NET déduit automatiquement les schémas et codes de
retour à partir des types concrets, alors que `IResult` produit un 200 opaque sans
schéma.

```csharp
// ✅ TypedResults — OpenAPI affiche 200 + schéma TaskDto
private static async Task<Ok<TaskDto>> GetTaskAsync(
    Guid id, ITaskStore store)
{
    TaskDto task = await store.GetAsync(id).ConfigureAwait(false);
    return TypedResults.Ok(task);
}

// ❌ Results — OpenAPI affiche 200 sans schéma
app.MapGet("/tasks/{id}", async (Guid id, ITaskStore store) =>
    Results.Ok(await store.GetAsync(id)));
```

Pour les endpoints avec plusieurs codes de retour, utiliser `Results<T1, T2>` :

```csharp
private static async Task<Results<Ok<TaskDto>, NotFound>> GetTaskAsync(
    Guid id, ITaskStore store)
{
    TaskDto? task = await store.FindAsync(id).ConfigureAwait(false);
    return task is not null
        ? TypedResults.Ok(task)
        : TypedResults.NotFound();
}
```

#### Types anonymes interdits

Ne jamais retourner de type anonyme (`new { count }`) dans un handler. Créer un record
typé pour que le schéma OpenAPI soit déterministe et nommé.

```csharp
// ✅ Record typé — OpenAPI affiche un schéma "UnreadCountResponse"
public sealed record UnreadCountResponse(int Count);

private static async Task<Ok<UnreadCountResponse>> GetUnreadCountAsync(...)
    => TypedResults.Ok(new UnreadCountResponse(count));

// ❌ Type anonyme — schéma OpenAPI non nommé et fragile
app.MapGet("/unread-count", () => Results.Ok(new { count = 42 }));
```

#### `.Produces<T>()` en fallback

Quand un handler ne peut pas utiliser `TypedResults` (logique de validation
complexe retournant `IResult`), déclarer les métadonnées manuellement :

```csharp
group.MapGet("/localization", GetLocalizationAsync)
    .Produces<ApplicationLocalizationResponse>();

group.MapPut("/overrides/{key}", SetOverrideAsync)
    .Produces(StatusCodes.Status204NoContent);
```

#### Réponses d'erreur — `ProblemDetails` obligatoire

Toutes les réponses d'erreur doivent utiliser `TypedResults.Problem()` (RFC 7807),
jamais `TypedResults.BadRequest<string>()` ou `TypedResults.BadRequest("message")`.
Retourner une chaîne brute produit un schéma `string` au lieu d'un `ProblemDetails`
structuré.

```csharp
// ✅ ProblemDetails structuré — schéma OpenAPI cohérent
return TypedResults.Problem(
    detail: "Invalid webhook payload.",
    statusCode: StatusCodes.Status400BadRequest);

// ❌ Chaîne brute — schéma string, pas de structure d'erreur standardisée
return TypedResults.BadRequest("Invalid webhook payload.");
```

Le type de retour du handler doit refléter `ProblemHttpResult` (pas `BadRequest<string>`) :

```csharp
private static Task<Results<Ok, ProblemHttpResult>> HandleWebhookAsync(...)
```

#### Codes HTTP sémantiques

| Opération | Code | Retour |
| --------- | ---- | ------ |
| Lecture | 200 | `Ok<T>` |
| Création | 201 | `Created` + header `Location` |
| Traitement asynchrone | 202 | `Accepted` + identifiant de suivi |
| Mise à jour / suppression sans body | 204 | `NoContent` |

> Voir [réponses HTTP](../../framework/api/http-responses.md) pour le détail de chaque
> code.

#### Métadonnées obligatoires sur chaque endpoint

| Méthode | Effet OpenAPI |
| ------- | ------------- |
| `.WithName("GetTask")` | `operationId` — utilisé par les clients générés |
| `.WithSummary("Returns a task by ID.")` | Ligne de résumé dans l'UI Scalar |
| `.WithTags("Tasks")` | Groupement par tag dans la documentation |

Les handlers **doivent** être des méthodes nommées statiques (pas des lambdas inline).
Cela améliore la lisibilité, permet l'inférence des types de retour par ASP.NET,
et facilite les tests unitaires.

```csharp
// ✅ Méthode nommée statique
group.MapGet("/{id:guid}", GetTaskAsync)
    .WithName("GetTask")
    .WithSummary("Returns a task by its unique identifier.");

// ❌ Lambda inline — pas d'inférence de type, illisible
group.MapGet("/{id:guid}", async (Guid id, ITaskStore store) =>
    TypedResults.Ok(await store.GetAsync(id)));
```

#### Transformers OpenAPI centralisés (`Granit.ApiDocumentation`)

`Granit.ApiDocumentation` enregistre automatiquement des transformers qui améliorent
le document OpenAPI généré. Ils s'appliquent à **tous** les endpoints sans annotation
manuelle.

| Transformer | Rôle |
| --- | --- |
| `JwtBearerSecuritySchemeTransformer` | Ajoute le schéma Bearer si JWT est configuré |
| `OAuth2SecuritySchemeTransformer` | Ajoute le schéma OAuth2 Authorization Code (Scalar UI) |
| `SecurityRequirementOperationTransformer` | `[AllowAnonymous]` → `security: [{}]`, protégé → schéma global |
| `ProblemDetailsSchemaDocumentTransformer` | Schéma `ProblemDetails` dans `components/schemas` |
| `ProblemDetailsResponseOperationTransformer` | Ajoute les réponses 4xx/5xx `ProblemDetails` |
| `InternalTypeSchemaDocumentTransformer` | Supprime `IFormFile`, `JsonElement` des schémas |
| `NullableIntSchemaOperationTransformer` | Corrige `type: [integer, string]` → `[integer, null]` |
| `ParameterDescriptionOperationTransformer` | Descriptions centralisées des paramètres well-known |
| `TenantHeaderOperationTransformer` | Header `X-Tenant-Id` sur les routes multi-tenant |
| `InternalApiDocumentTransformer` | Filtre les routes internes |

Pour ajouter une description de paramètre à un nouveau nom well-known, éditez
`ParameterDescriptionOperationTransformer.s_descriptions` — pas besoin d'annoter
chaque endpoint individuellement.

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
