# Jobs récurrents — Granit.BackgroundJobs

Scheduling durable et pilotable pour les tâches de fond récurrentes.
Les jobs sont de simples **messages Wolverine** — la planification est atomique via l'Outbox,
sans aucun doublon possible en cluster multi-nœuds.

| Package | Rôle |
| --- | --- |
| `Granit.BackgroundJobs` | Core provider-agnostique : scheduling Wolverine, store InMemory, `IBackgroundJobManager`, `IBackgroundJobStore` |
| `Granit.BackgroundJobs.EntityFrameworkCore` | Persistance EF Core : `BackgroundJobsDbContext`, table `scheduling_background_jobs` (SQL Server / PostgreSQL) |
| `Granit.BackgroundJobs.Endpoints` | Administration HTTP : endpoints Minimal API, politique d'autorisation `BackgroundJobs.Admin` |

## Concepts clés

Un job récurrent est un **message Wolverine ordinaire**, décoré avec `[RecurringJob]`.
Aucune interface à implémenter sur le message.

```text
[RecurringJob("0 * * * *", "hourly-cleanup")]
HourlyCleanupCommand                         ← message Wolverine
    └── HourlyCleanupHandler.Handle(...)     ← handler Wolverine standard

RecurringJobSchedulingMiddleware             ← injecté automatiquement
    BeforeAsync() → RecordExecutionStartAsync()
    AfterAsync()  → ScheduleAsync(nextMessage, nextOccurrence)   ← dans la même transaction Outbox
```

**Garantie anti-doublon :** le prochain message est inséré dans l'Outbox dans la même transaction
que le handler. Si le nœud crashe avant le commit, Wolverine redélivre le message courant —
le suivant n'a jamais été inséré, aucun doublon n'est créé.

## Installation

### 1 — Module

```csharp
[DependsOn(typeof(GranitBackgroundJobsModule))]
public sealed class MyAppModule : GranitModule { }
```

`GranitBackgroundJobsModule` dépend de `GranitWolverineModule`.
L'Outbox transactionnelle est fournie par `GranitWolverinePostgresqlModule` (référencé séparément).

### 2 — Configuration

```json
// appsettings.json
{
  "BackgroundJobs": {
    "Mode": "InMemory"
  }
}
```

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `Mode` | `JobStoreMode` | `InMemory` | `InMemory` (dev/tests) ou `Durable` (EF Core — production) |
| `ConnectionString` | `string` | — | Obligatoire quand `Mode = Durable` |

> **Mode `InMemory`** : état perdu au redémarrage. Parfait pour le développement et les tests.
>
> **Mode `Durable`** : store EF Core (SQL Server / PostgreSQL). L'état admin (pause, historique)
> survit aux redémarrages. Nécessite `Granit.BackgroundJobs.EntityFrameworkCore` (Story #128).

### Mode durable — EF Core

Ajouter le module EF Core et enregistrer le DbContext :

```csharp
[DependsOn(
    typeof(GranitBackgroundJobsModule),
    typeof(GranitBackgroundJobsEntityFrameworkCoreModule))]
public sealed class MyAppModule : GranitModule { }
```

La table `scheduling_background_jobs` est créée par la migration EF Core (Story #129).
Compatible SQL Server et PostgreSQL.

### 3 — Assemblies additionnelles (optionnel)

Si les messages de jobs sont déclarés dans des assemblies secondaires :

```csharp
builder.AddGranitBackgroundJobs(
    additionalAssemblies: [typeof(MyWorkerMessage).Assembly]);
```

## Déclarer un job

```csharp
// Message Wolverine + déclaration du scheduling
[RecurringJob("0 8 * * *", "daily-report")]
public sealed class GenerateDailyReportCommand;

// Handler Wolverine standard — aucun code de rescheduling nécessaire
public static class GenerateDailyReportHandler
{
    public static async Task Handle(
        GenerateDailyReportCommand cmd,
        IReportService reportService,
        CancellationToken ct)
    {
        await reportService.GenerateAsync(ct);
        // Le middleware injecte automatiquement ScheduleAsync() après le retour du handler.
    }
}
```

### Syntaxe cron

Cronos (bibliothèque Schedy) est utilisé pour le parsing.
Formats supportés : **5 champs** (sans secondes) et **6 champs** (avec secondes).

| Expression | Signification |
| --- | --- |
| `0 * * * *` | Toutes les heures (à la minute 0) |
| `0 8 * * *` | Tous les jours à 08:00 UTC |
| `0 8 * * 1` | Tous les lundis à 08:00 UTC |
| `*/5 * * * *` | Toutes les 5 minutes |
| `0 0 1 * *` | Le 1er de chaque mois à 00:00 UTC |
| `0 */30 * * * *` | Toutes les 30 secondes (6 champs) |

> Les occurrences sont calculées en **UTC**. Utiliser `ICurrentTimezoneProvider` si une
> conversion vers le fuseau horaire du tenant est nécessaire dans le handler.

## Administration — IBackgroundJobManager

```csharp
public interface IBackgroundJobManager
{
    Task<IReadOnlyList<BackgroundJobStatus>> GetAllAsync(CancellationToken ct = default);
    Task<BackgroundJobStatus?> FindAsync(string jobName, CancellationToken ct = default);
    Task PauseAsync(string jobName, CancellationToken ct = default);
    Task ResumeAsync(string jobName, CancellationToken ct = default);
    Task TriggerNowAsync(string jobName, CancellationToken ct = default);
}
```

**`PauseAsync`** : désactive `IsEnabled` dans le store. Le middleware arrête de planifier
la prochaine occurrence après la prochaine exécution.

**`ResumeAsync`** : réactive `IsEnabled` et planifie immédiatement la prochaine occurrence
via l'`IMessageBus`.

**`TriggerNowAsync`** : publie le message immédiatement (sans délai). Injecte le header
`X-Triggered-By` avec le `UserId` de l'opérateur pour la piste d'audit HDS.

### BackgroundJobStatus

```csharp
public sealed record BackgroundJobStatus(
    string JobName,
    string CronExpression,
    bool IsEnabled,
    DateTimeOffset? LastExecutedAt,
    DateTimeOffset? NextExecutionAt,
    int ConsecutiveFailures,
    long DeadLetterCount,
    string? LastError);
```

## Piste d'audit HDS

| Champ | Source | Valeur |
| --- | --- | --- |
| `LastExecutedAt` | `BeforeAsync()` | UTC de début d'exécution |
| `NextExecutionAt` | `AfterAsync()` | UTC de la prochaine occurrence calculée |
| `ConsecutiveFailures` | handler failure | Incrémenté à chaque erreur, remis à 0 en cas de succès |
| `TriggeredBy` | header `X-Triggered-By` | `UserId` de l'opérateur si déclenchement manuel |

`TriggeredBy` est write-once par cycle d'exécution et conservé pour audit.
Il n'est jamais un identifiant nominatif (UserId de l'IdP, non PII direct).

## Endpoints d'administration

Le package `Granit.BackgroundJobs.Endpoints` expose 5 routes Minimal API protégées
par la politique `BackgroundJobs.Admin`.

### Enregistrement

```csharp
app.MapBackgroundJobsEndpoints();

// Avec options personnalisées
app.MapBackgroundJobsEndpoints(opts =>
{
    opts.RoutePrefix  = "admin/jobs";          // défaut : "background-jobs"
    opts.RequiredRole = "ops-team";            // défaut : "granit-background-jobs-admin"
    opts.TagName      = "Background Jobs";     // défaut : "Background Jobs"
});
```

Le module doit être déclaré dans l'application hôte :

```csharp
[DependsOn(
    typeof(GranitBackgroundJobsModule),
    typeof(GranitBackgroundJobsEndpointsModule))]
public sealed class MyAppModule : GranitModule { }
```

### Routes

| Méthode | Route | Réponse | Description |
| --- | --- | --- | --- |
| `GET` | `/{prefix}` | `200 Ok<IReadOnlyList<BackgroundJobStatus>>` | Liste tous les jobs |
| `GET` | `/{prefix}/{name}` | `200 Ok<BackgroundJobStatus>` / `404` | Détail d'un job |
| `POST` | `/{prefix}/{name}/pause` | `204` / `404` | Suspend le scheduling |
| `POST` | `/{prefix}/{name}/resume` | `204` / `404` | Relance le scheduling |
| `POST` | `/{prefix}/{name}/trigger` | `202 Accepted` / `404` | Exécution immédiate |

### Sécurisation — couches de protection

#### 1 — Authentification (JWT Keycloak)

L'application hôte doit charger `GranitAuthenticationKeycloakModule` (ou
`GranitJwtBearerModule`). Aucune configuration supplémentaire n'est nécessaire
dans `Granit.BackgroundJobs.Endpoints` — les endpoints rejettent automatiquement
les requêtes sans token valide (`401`).

#### 2 — Autorisation (système de permissions Granit)

`GranitBackgroundJobsEndpointsModule` enregistre `BackgroundJobsPermissionDefinitionProvider`,
qui déclare la permission `BackgroundJobs.Admin` dans le registre de permissions Granit.

Lorsque `GranitAuthorizationModule` est chargé (toujours le cas via `[DependsOn]`),
`DynamicPermissionPolicyProvider` intercepte la politique `BackgroundJobs.Admin` et
active le pipeline complet `IPermissionChecker` :

```text
Requête → DynamicPermissionPolicyProvider → PermissionRequirement("BackgroundJobs.Admin")
  → IPermissionChecker.IsGrantedAsync("BackgroundJobs.Admin")
      1. AlwaysAllow = true  → accordé  (dev/tests uniquement)
      2. AdminRoles bypass   → accordé  (root of trust, sans DB)
      3. Cache               → hit ou miss
      4. IPermissionGrantStore.IsGrantedAsync(role, "BackgroundJobs.Admin")
```

#### 3 — Configurer l'accès en production

**Option A — AdminRoles bypass (simple)** : ajouter le rôle Keycloak des opérateurs
dans `GranitAuthorizationOptions.AdminRoles`. Aucune table DB nécessaire.

```json
// appsettings.json
{
  "Authorization": {
    "AdminRoles": ["admin", "granit-background-jobs-admin"]
  }
}
```

**Option B — IPermissionManager (contrôle fin par tenant)** : accorder la permission
au démarrage (nécessite `Granit.Authorization.EntityFrameworkCore`) :

```csharp
// Program.cs / hosted service
await permissionManager.SetAsync(
    "BackgroundJobs.Admin",
    "granit-background-jobs-admin",
    tenantId: null,   // null = toutes les tenants
    isGranted: true);
```

#### 4 — Tests sans GranitAuthorizationModule

En tests unitaires qui n'utilisent pas le module Granit (plain `AddAuthorization()`),
`DynamicPermissionPolicyProvider` n'est pas actif et la politique tombe en fallback
sur le `RequireRole()` enregistré par `MapBackgroundJobsEndpoints()` :

```csharp
builder.Services.AddAuthorization();  // sans GranitAuthorizationModule
app.MapBackgroundJobsEndpoints(opts => opts.RequiredRole = "granit-background-jobs-admin");
// → RequireRole("granit-background-jobs-admin") actif
```

## Store personnalisé — IBackgroundJobStore

`IBackgroundJobStore` est une interface **publique** permettant de fournir une
implémentation de persistance alternative (Redis, MongoDB, etc.) :

```csharp
public interface IBackgroundJobStore
{
    Task<BackgroundJobDefinition?> FindAsync(string jobName, CancellationToken ct = default);
    Task<IReadOnlyList<BackgroundJobDefinition>> GetEnabledJobsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BackgroundJobDefinition>> GetAllJobsAsync(CancellationToken ct = default);
    Task SeedJobsAsync(IEnumerable<RecurringJobRegistration> registrations, CancellationToken ct = default);
    Task RecordExecutionStartAsync(string jobName, DateTimeOffset startedAt, CancellationToken ct = default);
    Task RecordNextExecutionAsync(string jobName, DateTimeOffset nextExecution, CancellationToken ct = default);
    Task RecordExecutionFailureAsync(string jobName, string errorMessage, CancellationToken ct = default);
    Task SetEnabledAsync(string jobName, bool enabled, CancellationToken ct = default);
    Task SetTriggeredByAsync(string jobName, string? triggeredBy, CancellationToken ct = default);
}
```

Deux implémentations sont fournies :

| Implémentation | Package | Mode |
| --- | --- | --- |
| `InMemoryBackgroundJobStore` | `Granit.BackgroundJobs` | `JobStoreMode.InMemory` |
| `EfBackgroundJobStore` | `Granit.BackgroundJobs.EntityFrameworkCore` | `JobStoreMode.Durable` |

Pour une implémentation Redis ou MongoDB, enregistrer le service **en Singleton** :

```csharp
services.AddSingleton<IBackgroundJobStore, RedisBackgroundJobStore>();
```

## Architecture interne

```text
[Startup]
  RecurringJobDiscovery.Discover(assemblies)       → IReadOnlyList<RecurringJobRegistration>
  BackgroundJobsSeedService.StartAsync()           → IBackgroundJobStore.SeedJobsAsync()

[WolverineOptions]
  opts.Policies.AddMiddleware<RecurringJobSchedulingMiddleware>(
      chain => chain.MessageType.GetCustomAttribute<RecurringJobAttribute>() is not null)
  services.AddSingularAgent<CronSchedulerAgent>()  ← agent singleton cluster-safe

[Cluster — CronSchedulerAgent]
  startAsync()
    → store.GetEnabledJobsAsync()
    → si NextExecutionAt > Now : skip (déjà planifié via Outbox)
    → Cronos.GetNextOccurrence()
    → bus.ScheduleAsync(message, next)
    → store.RecordNextExecutionAsync()

[Runtime — par message récurrent]
  RecurringJobSchedulingMiddleware.BeforeAsync()
    → store.RecordExecutionStartAsync()
    → store.SetTriggeredByAsync()  ← si X-Triggered-By présent
  [Handler]
  RecurringJobSchedulingMiddleware.AfterAsync()
    → Cronos.GetNextOccurrence()
    → context.ScheduleAsync(nextMessage, next)   ← Outbox, même transaction
    → store.RecordNextExecutionAsync()
```

## Roadmap

| Story | Statut | Description |
| --- | --- | --- |
| #127 | ✅ Terminé | `BackgroundJobDefinition` EF Core entity + `BackgroundJobsDbContext` (package `Granit.BackgroundJobs.EntityFrameworkCore`) |
| #128 | ✅ Terminé | `EfBackgroundJobStore` (SQL Server / PostgreSQL) + `AddGranitBackgroundJobsEntityFrameworkCore()` |
| #129 | ✅ Terminé | Migrations EF Core + schéma `scheduling_background_jobs` |
| #130 | ✅ Terminé | Intégration `GranitWolverinePostgresqlModule` |
| #136 | ✅ Terminé | `CronSchedulerAgent` (`SingularAgent`) — démarrage cluster-safe, anti-doublon |
| #137 | ✅ Terminé | Étendre `OutgoingContextMiddleware` pour propager `X-Triggered-By` |
| #138 | ✅ Terminé | Tests d'intégration `BackgroundJobsIntegrationTests` |
| #140 | ✅ Terminé | Scaffolding `Granit.BackgroundJobs.Endpoints` — module, options, `MapBackgroundJobsEndpoints()` |
| #141 | ✅ Terminé | GET /background-jobs + GET /background-jobs/{name} (TypedResults, OpenAPI) |
| #142 | ✅ Terminé | POST pause / resume / trigger (204, 202 Accepted, 404) |
| #143 | ✅ Terminé | Policy `BackgroundJobs.Admin` — `RequiredRole` configurable via options |
| #144 | ✅ Terminé | 17 tests d'intégration — 401/403/404, désérialisation JSON, custom role |
| #134 | ✅ Terminé | `DeadLetterCount` via `IMessageStore` — `BackgroundJobManager.GetAllAsync()` intègre les stats DLQ Wolverine (dégradation gracieuse si `IMessageStore` absent) |

## Conformité HDS

- La planification est **atomique** : le prochain message est dans l'Outbox Wolverine,
  même transaction que le handler. Pas de perte possible en cas de crash.
- `TriggerNowAsync` trace l'opérateur via `X-Triggered-By` (champ `TriggeredBy` du store).
- La chaîne de connexion du store durable doit pointer sur une base **en Europe (OVHcloud FR)**,
  jamais sur un service soumis au Cloud Act américain.
- Les données de scheduling (cron, horodatages) ne contiennent aucune donnée de santé (DPS).

## Dépendances Granit

| Direction | Modules |
|-----------|---------|
| **Dépend de** | `Granit.Core`, `Granit.Security`, `Granit.Timing`, `Granit.Wolverine` |
| **Utilisé par** | `Granit.BackgroundJobs.EntityFrameworkCore`, `Granit.BackgroundJobs.Endpoints` |
| **Package Endpoints** | `Granit.BackgroundJobs.Endpoints` → ajoute `Granit.Authorization` |

> Voir le [graphe de dépendances complet](../dependencies.md).
