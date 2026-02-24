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
`[MigrationCycle]`. La phase 2 est orchestrée par Wolverine via
`RunMigrationBatchCommand`.

## Installation

```bash
dotnet add package Granit.Persistence.Migrations
```

## Configuration

### Avec le système de modules

```csharp
// Program.cs
[DependsOn(typeof(GranitPersistenceMigrationsModule))]
public sealed class MyAppModule : GranitModule { }
```

```csharp
// Startup — connexion au provider PostgreSQL
builder.AddGranitPersistenceMigrations(opts => opts.UseNpgsql(connectionString));
```

### Enregistrement direct (sans modules)

```csharp
builder.AddGranitPersistenceMigrations(opts => opts.UseNpgsql(connectionString));
```

> La méthode `AddGranitPersistenceMigrations` enregistre :
>
> - `MigrationProgressDbContext` — suivi des cycles dans `granit_migration_progress`
> - `IMigrationCycleRegistry` — registre singleton des cycles
> - `ITenantDbIsolator` — no-op par défaut (shared DB et DB-per-tenant)

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

La migration est déclenchée en envoyant un `RunMigrationBatchCommand` via Wolverine :

```csharp
await bus.SendAsync(new RunMigrationBatchCommand(
    CycleId:   "patient-fullname-v2",
    TenantId:  Guid.Empty,          // Guid.Empty pour les apps mono-tenant
    Cursor:    null,                 // null pour démarrer depuis le début
    BatchSize: 500));
```

Le handler `RunMigrationBatchHandler` cascade automatiquement le message suivant
tant que `NextCursor != null`.

## Multi-tenant

| Topologie | Comportement | Configuration |
| --------- | ------------ | ------------- |
| **Shared DB** (colonne TenantId) | No-op — les query filters EF Core filtrent automatiquement | Aucune |
| **Tenant-per-Database** | No-op — la connexion est déjà résolue par `PerTenantDbContextFactory` | Aucune |
| **Tenant-per-Schema** | `SET search_path = schema_{tenantId}` avant chaque batch | Implémenter `ITenantDbIsolator` |

### Isolation Tenant-per-Schema

```csharp
// Enregistrer avant AddGranitPersistenceMigrations()
builder.Services.AddSingleton<ITenantDbIsolator, MySchemaIsolator>();
builder.AddGranitPersistenceMigrations(opts => opts.UseNpgsql(connectionString));
```

```csharp
public sealed class MySchemaIsolator : ITenantDbIsolator
{
    public async Task IsolateAsync(DbContext context, Guid tenantId, CancellationToken ct)
    {
        string schema = $"schema_{tenantId:N}";
        await context.Database.ExecuteSqlAsync(
            $"SET search_path = {schema}", ct);
    }
}
```

## Suivi de progression

La table `granit_migration_progress` contient une ligne par cycle et par tenant.

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

## Garanties et contraintes

- **Idempotence obligatoire** : le délégué doit tolérer une réexécution sur des
  lignes déjà migrées (pattern `WHERE new_column IS NULL`).
- **Durabilité** : la cascade Wolverine est persistée dans l'Outbox ; une panne
  redémarre le batch suivant au `LastCursor`.
- **Pas de Polly** : la résilience est assurée par la politique de retry Wolverine
  (`OnAnyException().RetryWithCooldown(...)`).
- **Pas de `COUNT(*)`** : `TotalRows` n'est jamais calculé automatiquement
  (risque de lock sur les grandes tables HDS). Setter manuellement si nécessaire.
