# Migrations Zero-Downtime (Expand & Contract)

`Granit.Persistence.Migrations` fournit un framework de migrations de données
sans interruption de service, basé sur le patron **Expand & Contract**.

## Migrations classiques vs Zero-Downtime

| Critère | Migrations EF Core classiques | Zero-Downtime (Expand & Contract) |
| ------- | ----------------------------- | ---------------------------------- |
| **Interruption de service** | Nécessaire (arrêt pendant la migration) | Aucune — déployable en rolling update |
| **Complexité** | Faible | Élevée (3 phases, suivi de progression) |
| **Volume de données** | Adapté à tous volumes | Obligatoire pour les grands volumes |
| **Compliance HDS** | Acceptable pour les systèmes non critiques | Requis pour les services 24h/24 |
| **Rollback** | Difficile si données modifiées | Naturel — les deux colonnes coexistent en phase Migrate |
| **Cas d'usage** | Nouvelles tables, colonnes facultatives, petits volumes | Renommage de colonnes, changements de type, millions de lignes |

> **Règle** : les migrations EF Core classiques restent valides pour les
> applications non critiques (outils internes, services à faible disponibilité).
> Pour les services HDS soumis à des SLA élevés, préférez Expand & Contract.

## Le patron Expand & Contract

```text
Phase 1 — Expand   : ajout de la nouvelle colonne (nullable / valeur par défaut)
Phase 2 — Migrate  : backfill en arrière-plan, batch par batch
Phase 3 — Contract : suppression de l'ancienne colonne
```

Les phases 1 et 3 correspondent à des migrations EF Core normales annotées avec
`[MigrationCycle]`. La phase 2 est orchestrée par `IMigrationBatchDispatcher` via
`RunMigrationBatchCommand`.

## Installation

```bash
# Core — dispatch via Channel<T> intégré (sans Wolverine)
dotnet add package Granit.Persistence.Migrations

# Optionnel — dispatch via Outbox Wolverine (durable, at-least-once)
dotnet add package Granit.Persistence.Migrations.Wolverine
```

## Configuration

### Avec le système de modules

```csharp
// Program.cs — dispatch Channel (défaut)
[DependsOn(typeof(GranitPersistenceMigrationsModule))]
public sealed class MyAppModule : GranitModule { }

// Program.cs — dispatch Wolverine Outbox (durable)
[DependsOn(typeof(GranitPersistenceMigrationsWolverineModule))]
public sealed class MyAppModule : GranitModule { }
```

> **Note** : les exemples utilisent `UseNpgsql()` (PostgreSQL). Granit est agnostique :
> tout provider EF Core est supporté (`UseSqlServer()`, `UseSqlite()`, etc.).

```csharp
// Startup — connexion au provider (exemple PostgreSQL)
builder.AddGranitPersistenceMigrations(opts => opts.UseNpgsql(connectionString));
```

### Enregistrement direct (sans modules)

```csharp
builder.AddGranitPersistenceMigrations(opts => opts.UseNpgsql(connectionString));
```

> La méthode `AddGranitPersistenceMigrations` enregistre :
>
> - `MigrationProgressDbContext` — suivi des cycles dans `data_migration_progress`
> - `IMigrationCycleRegistry` — registre singleton des cycles
> - `ITenantDbIsolator` — no-op par défaut (shared DB et DB-per-tenant)
> - `ITenantEnumerator` — no-op par défaut (retourne un flux vide)
> - `IMigrationBatchDispatcher` — dispatch des commandes (Channel par défaut)
> - `MigrationBatchWorker` — `BackgroundService` consommant le channel
> - `MigrationStartupService` — service hébergé de reprise au démarrage
> - `MigrationStartupOptions` — options liées depuis la section `GranitMigrations`

## Déclaration d'un cycle

```csharp
// À l'initialisation de l'application
IMigrationCycleRegistry registry = app.Services
    .GetRequiredService<IMigrationCycleRegistry>();

registry.Register<MyDbContext>("patient-fullname-v2", async (context, batch, ct) =>
{
    // La condition WHERE new_column IS NULL garantit l'idempotence.
    int count = await context.Database.ExecuteSqlAsync(
        $"""
        UPDATE patients
        SET    full_name_v2 = first_name || ' ' || last_name
        WHERE  full_name_v2 IS NULL
        AND    id > {batch.Cursor ?? Guid.Empty}
        ORDER BY id
        LIMIT  {batch.Size}
        """, ct);

    // Retourner le curseur suivant, ou null si plus rien à traiter.
    Guid? nextCursor = count == batch.Size
        ? await GetLastProcessedIdAsync(context, ct)
        : null;

    return new MigrationBatchResult(count, nextCursor?.ToString());
});
```

### API fluente

```csharp
registry
    .Register<MyDbContext>("cycle-a", delegateA)
    .Register<MyDbContext>("cycle-b", delegateB);
```

## Déclenchement de la migration

La migration est déclenchée en envoyant un `RunMigrationBatchCommand` via
`IMigrationBatchDispatcher` :

```csharp
await dispatcher.DispatchAsync(new RunMigrationBatchCommand(
    CycleId:   "patient-fullname-v2",
    TenantId:  Guid.Empty,          // Guid.Empty pour les apps mono-tenant
    Cursor:    null,                 // null pour démarrer depuis le début
    BatchSize: 500));
```

Le `MigrationBatchWorker` (Channel) ou le `RunMigrationBatchHandler` (Wolverine)
cascade automatiquement le batch suivant tant que `NextCursor != null`.

## Reprise au démarrage

`MigrationStartupService` est un service hébergé qui, au démarrage de l'application,
interroge la table `data_migration_progress` et publie un `RunMigrationBatchCommand`
pour chaque cycle en statut `Pending` ou `InProgress`.

Le comportement dépend de `ITenantEnumerator` :

- **Enumerateur vide** (défaut) : un message par ligne de progression, le `TenantId` stocké
  dans la ligne est utilisé (`null` → `Guid.Empty`).
- **Enumerateur personnalisé** : un message par tenant par cycle, le curseur stocké permet
  la reprise. Les tenants sans ligne de progression démarrent depuis le début (`Cursor = null`).

Les exceptions sont capturées et logguées ; le démarrage de l'application n'est jamais bloqué.

### Options de démarrage

```json
// appsettings.json
{
  "GranitMigrations": {
    "DefaultBatchSize": 500,
    "BatchExecutionTimeout": "00:05:00"
  }
}
```

| Propriété | Type | Défaut | Description |
| --------- | ---- | ------ | ----------- |
| `DefaultBatchSize` | `int` | `500` | Nombre de lignes par batch |
| `BatchExecutionTimeout` | `TimeSpan` | `5 min` | Timeout de sécurité par batch (prévient les hangs infinis) |

## Multi-tenant

| Topologie | `ITenantDbIsolator` | `ITenantEnumerator` |
| --------- | ------------------- | ------------------- |
| **Single-tenant** | No-op (défaut) | No-op (défaut) |
| **Shared DB** (colonne TenantId) | No-op (défaut) | No-op (défaut) |
| **Tenant-per-Database** | No-op (défaut) | No-op (défaut) |
| **Tenant-per-Schema** | Implémenter `ITenantDbIsolator` | Implémenter `ITenantEnumerator` |

### Isolation Tenant-per-Schema

```csharp
// Enregistrer avant AddGranitPersistenceMigrations()
builder.Services.AddSingleton<ITenantDbIsolator, MySchemaIsolator>();
builder.Services.AddSingleton<ITenantEnumerator, MyTenantEnumerator>();
builder.AddGranitPersistenceMigrations(opts => opts.UseNpgsql(connectionString));
```

```csharp
public sealed class MySchemaIsolator : ITenantDbIsolator
{
    public async Task IsolateAsync(DbContext context, Guid tenantId, CancellationToken cancellationToken)
    {
        string schema = $"schema_{tenantId:N}";
        await context.Database.ExecuteSqlAsync(
            $"SET search_path = {schema}", ct);
    }
}
```

```csharp
public sealed class MyTenantEnumerator : ITenantEnumerator
{
    private readonly ITenantRepository _repository;

    public MyTenantEnumerator(ITenantRepository repository) => _repository = repository;

    public async IAsyncEnumerable<Guid> GetActiveTenantIdsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (Guid id in _repository.GetActiveIdsAsync(ct))
        {
            yield return id;
        }
    }
}
```

## Suivi de progression

La table `data_migration_progress` contient une ligne par cycle et par tenant.

| Colonne | Description |
| ------- | ----------- |
| `cycle_id` | Identifiant du cycle (ex. `patient-fullname-v2`) |
| `phase` | Phase courante (0 = Expand, 1 = Migrate, 2 = Contract) |
| `status` | Statut (0 = Pending, 1 = InProgress, 2 = Completed, 3 = Failed) |
| `processed_rows` | Nombre total de lignes migrées |
| `last_cursor` | Curseur JSON de reprise |
| `tenant_id` | NULL pour les apps mono-tenant |
| `error` | Message d'erreur (tronqué à 4 000 caractères) |
| `started_at` | Début de la phase Migrate |
| `completed_at` | Fin de la phase Migrate |

> Les mises à jour de progression sont **best-effort** : elles sont commitées
> indépendamment de la transaction de données tenant et ne bloquent jamais
> le traitement du batch en cas d'échec.

## Arrêt gracieux (Graceful Shutdown)

Le `MigrationBatchWorker` gère l'arrêt propre via **deux niveaux de CancellationToken** :

1. **`stoppingToken`** (BackgroundService) : annulé lors d'un SIGTERM Kubernetes.
   Arrête la lecture du channel (pas de nouveau batch accepté).
2. **`BatchExecutionTimeout`** : CTS interne par batch (défaut : 5 min).
   Protège contre les hangs infinis.

Le batch **en cours d'exécution n'est jamais interrompu** : il termine son
`SaveChangesAsync()` et sauvegarde la progression avant que le worker ne s'arrête.
Au prochain démarrage, `MigrationStartupService` reprend depuis le `LastCursor`.

## Garanties et contraintes

- **Idempotence obligatoire** : le délégué doit tolérer une réexécution sur des
  lignes déjà migrées (pattern `WHERE new_column IS NULL`).
- **Durabilité (Channel)** : la progression est persistée après chaque batch ;
  une panne redémarre au `LastCursor` au prochain démarrage.
- **Durabilité (Wolverine)** : la cascade est persistée dans l'Outbox PostgreSQL ;
  une panne redémarre le batch suivant automatiquement (at-least-once).
- **Pas de `COUNT(*)`** : `TotalRows` n'est jamais calculé automatiquement
  (risque de lock sur les grandes tables HDS). Setter manuellement si nécessaire.

## Analyseurs Roslyn

Le package `Granit.Analyzers` impose les conventions Expand & Contract au build
et dans l'IDE via quatre règles Roslyn :

| Règle | Sévérité | Description |
| ----- | -------- | ----------- |
| GRMIGA001 | Error | `DropColumn` sans `[MigrationCycle(MigrationPhase.Contract, ...)]` |
| GRMIGA002 | Error | `RenameColumn` interdit — utiliser AddColumn + DropColumn |
| GRMIGA003 | Warning | `AddColumn` NOT NULL sans `defaultValue` ni `defaultValueSql` |
| GRMIGA004 | Warning | `AlterColumn` avec changement de type sans annotation Contract |

Ces règles s'activent automatiquement quand `Granit.Persistence.Migrations` est
référencé (opt-in). Voir la [documentation complète des analyseurs](../diagnostics/analyzers.md)
pour le détail de chaque règle, les exemples et la suppression des diagnostics.

## Dépendances Granit

### `Granit.Persistence.Migrations` (core)

| Direction       | Modules                                              |
| --------------- | ---------------------------------------------------- |
| **Dépend de**   | `Granit.Core`, `Granit.Persistence`, `Granit.Timing` |
| **Utilisé par** | `Granit.Persistence.Migrations.Wolverine`            |

### `Granit.Persistence.Migrations.Wolverine` (optionnel)

| Direction       | Modules                                             |
| --------------- | --------------------------------------------------- |
| **Dépend de**   | `Granit.Persistence.Migrations`, `Granit.Wolverine` |
| **Utilisé par** | Module feuille                                      |

> Voir le [graphe de dépendances complet](../dependencies.md).
