# Jobs récurrents — Granit.BackgroundJobs

Scheduling durable et pilotable pour les tâches de fond récurrentes.
Les jobs sont de simples **messages Wolverine** — la planification est atomique via l'Outbox,
sans aucun doublon possible en cluster multi-nœuds.

| Package | Rôle |
| --- | --- |
| `Granit.BackgroundJobs` | Core provider-agnostique : scheduling Wolverine, store InMemory, `IBackgroundJobManager` |
| `Granit.BackgroundJobs.EntityFrameworkCore` | Persistance EF Core : `BackgroundJobsDbContext`, table `granit_background_jobs` (SQL Server / PostgreSQL) |

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

La table `granit_background_jobs` est créée par la migration EF Core (Story #129).
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

## Architecture interne

```text
[Startup]
  RecurringJobDiscovery.Discover(assemblies)       → IReadOnlyList<RecurringJobRegistration>
  BackgroundJobsSeedService.StartAsync()           → IBackgroundJobStore.SeedJobsAsync()

[WolverineOptions]
  opts.Policies.AddMiddleware<RecurringJobSchedulingMiddleware>(
      chain => chain.MessageType.GetCustomAttribute<RecurringJobAttribute>() is not null)

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
| #129 | ✅ Terminé | Migrations EF Core + schéma `granit_background_jobs` |
| #130 | ✅ Terminé | Intégration `GranitWolverinePostgresqlModule` |
| #136 | ✅ Terminé | `CronSchedulerAgent` (`SingularAgent`) — démarrage cluster-safe, anti-doublon |
| #137 | En cours | Étendre `OutgoingContextMiddleware` pour propager `X-Triggered-By` |
| #134 | Planifié | `DeadLetterCount` via `IMessageStore` |
| #140–144 | Planifié | `Granit.BackgroundJobs.Endpoints` — API Minimal, policy `BackgroundJobs.Admin` |

## Conformité HDS

- La planification est **atomique** : le prochain message est dans l'Outbox Wolverine,
  même transaction que le handler. Pas de perte possible en cas de crash.
- `TriggerNowAsync` trace l'opérateur via `X-Triggered-By` (champ `TriggeredBy` du store).
- La chaîne de connexion du store durable doit pointer sur une base **en Europe (OVHcloud FR)**,
  jamais sur un service soumis au Cloud Act américain.
- Les données de scheduling (cron, horodatages) ne contiennent aucune donnée de santé (DPS).
