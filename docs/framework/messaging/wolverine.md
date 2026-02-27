# Messagerie — Granit.Wolverine

Intégration de [WolverineFx](https://wolverinefx.net/) pour les applications Digital Dynamics.
Deux packages composables :

| Package | Rôle |
| --- | --- |
| `Granit.Wolverine` | Core provider-agnostique : module, routing IDomainEvent, propagation de contexte |
| `Granit.Wolverine.Postgresql` | Outbox PostgreSQL + intégration transactionnelle EF Core |

## Installation rapide

```csharp
// Module — ajoute tout (Outbox PostgreSQL inclus)
[DependsOn(typeof(GranitWolverinePostgresqlModule))]
public sealed class MyAppModule : GranitModule { }
```

```json
// appsettings.json
{
  "Wolverine": {
    "MaxRetryAttempts": 3,
    "RetryDelays": ["00:00:05", "00:00:30", "00:05:00"]
  },
  "WolverinePostgresql": {
    "TransportConnectionString": "Host=db;Database=myapp;Username=app;Password=..."
  }
}
```

## Modules

### GranitWolverineModule

Enregistre le core Wolverine sans persistance.

```csharp
[DependsOn(typeof(GranitSecurityModule), typeof(GranitMultiTenancyModule))]
public sealed class GranitWolverineModule : GranitModule { ... }
```

Configure :

- Routing local des `IDomainEvent` → queue `"domain-events"` (jamais routés vers des transports externes)
- Politique de retry globale lue depuis `WolverineMessagingOptions`
- Propagation de contexte : `OutgoingContextMiddleware`, `TenantContextBehavior`, `UserContextBehavior`, `TraceContextBehavior`
- `WolverineCurrentUserService` comme implémentation de `ICurrentUserService`

### GranitWolverinePostgresqlModule

```csharp
[DependsOn(typeof(GranitWolverineModule), typeof(GranitPersistenceModule))]
public sealed class GranitWolverinePostgresqlModule : GranitModule { ... }
```

Ajoute :

- Outbox PostgreSQL durable (at-least-once delivery, conforme HDS)
- Intégration transactionnelle EF Core (`UseEntityFrameworkCoreTransactions`)
- Application automatique des transactions sur tous les handlers (`AutoApplyTransactions`)

> **Prérequis :** les `DbContext` participant aux transactions Wolverine doivent être enregistrés
> via `services.AddDbContextWithWolverineIntegration<TContext>()` et non `AddDbContext<TContext>()`.

## Options de configuration

### WolverineMessagingOptions (`"Wolverine"`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `MaxRetryAttempts` | `int` | `3` | Nombre maximum de tentatives de retry |
| `RetryDelays` | `TimeSpan[]` | `[5 s, 30 s, 5 min]` | Délais entre chaque tentative |

### WolverinePostgresqlOptions (`"WolverinePostgresql"`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `TransportConnectionString` | `string` | — | Chaîne de connexion PostgreSQL (obligatoire) |
| `TransactionMode` | `TransactionMiddlewareMode` | `Eager` | Mode de transaction EF Core |

`TransactionMiddlewareMode.Eager` est le mode HDS-conforme : la transaction est ouverte
explicitement avant tout write. Ne pas utiliser `Lightweight` en production.

## Propagation du contexte

Lors de l'émission d'un message, trois headers sont injectés automatiquement dans l'enveloppe :

| Header | Source | Comportement |
| --- | --- | --- |
| `X-Tenant-Id` | `ICurrentTenant.Id` | Omis si aucun tenant actif |
| `X-User-Id` | `ICurrentUserService.UserId` | Omis si non authentifié |
| `traceparent` | `Activity.Current?.Id` (W3C) | Omis si aucune trace OTel active |

À la réception, les behaviors restaurent le contexte avant l'exécution du handler :

```text
[Incoming message]
  → TenantContextBehavior.Before()    — restaure ICurrentTenant via AsyncLocal
  → UserContextBehavior.Before()      — restaure ICurrentUserService via AsyncLocal
  → TraceContextBehavior.Before()     — démarre une activity bridge liée au trace-id d'origine
  → [Handler]
  → TraceContextBehavior.After()      — dispose l'activity bridge
  → UserContextBehavior.After()       — dispose le scope
  → TenantContextBehavior.After()     — dispose le scope
```

```mermaid
sequenceDiagram
    participant API as Endpoint HTTP
    participant OCM as OutgoingContextMiddleware
    participant WOL as Wolverine Bus
    participant OB as Outbox PostgreSQL
    participant TX as Transaction EF Core
    participant TCB as TenantContextBehavior
    participant UCB as UserContextBehavior
    participant TrCB as TraceContextBehavior
    participant H as Handler Background

    API->>OCM: PublishAsync(event)
    OCM->>OCM: Injecte X-Tenant-Id, X-User-Id, traceparent
    OCM->>WOL: Envelope avec headers
    WOL->>OB: INSERT INTO outbox (même transaction)
    WOL->>TX: COMMIT
    Note over OB,TX: Atomique — at-least-once delivery

    OB->>TCB: Dispatch message
    TCB->>TCB: Restaure ICurrentTenant (AsyncLocal)
    TCB->>UCB: next()
    UCB->>UCB: Restaure ICurrentUserService (AsyncLocal)
    UCB->>TrCB: next()
    TrCB->>TrCB: Démarre Activity bridge (trace-id)
    TrCB->>H: Exécute handler
    H-->>TrCB: Résultat
    TrCB-->>UCB: Dispose activity
    UCB-->>TCB: Dispose scope
    TCB-->>OB: Terminé
```

La propagation du `traceparent` permet de corréler visuellement une requête HTTP et tous
ses traitements Wolverine asynchrones dans Grafana/Tempo sous un même `trace-id`.

→ Voir [diagnostics/wolverine-tracing.md](../diagnostics/wolverine-tracing.md) pour le détail.

### WolverineCurrentUserService

Remplace `ICurrentUserService` dans le conteneur DI. Résolution du `UserId` par ordre de priorité :

1. **Override AsyncLocal** — activé par `UserContextBehavior` dans les handlers background
2. **Fallback `IHttpContextAccessor`** — claims JWT pour les requêtes HTTP normales

Garantit que `AuditedEntityInterceptor` enregistre toujours un `ModifiedBy` non null dans
la piste d'audit HDS, même depuis un handler background.

## Écrire un handler

```csharp
// IDomainEvent → routé automatiquement en local
public sealed record OrderCreatedEvent(Guid OrderId) : IDomainEvent;

// Handler — le contexte tenant/utilisateur est restauré automatiquement
public sealed class OrderCreatedHandler
{
    public async Task Handle(OrderCreatedEvent evt, ICurrentTenant tenant, IMessageBus bus)
    {
        // tenant.Id est disponible ici, même en background
        await bus.PublishAsync(new SendConfirmationEmail(evt.OrderId));
    }
}
```

## Conformité HDS

- L'Outbox PostgreSQL garantit la livraison at-least-once sans perte de messages en cas de crash
- `TransactionMiddlewareMode.Eager` assure l'atomicité entre le write EF Core et la mise en queue
- La propagation `X-User-Id` garantit la traçabilité des opérations asynchrones dans l'audit trail
- La chaîne de connexion doit pointer sur une base de données **en Europe (OVHcloud FR)**,
  jamais sur un service US (Cloud Act)

## Dépendances Granit

| Direction | Modules |
|-----------|---------|
| **Dépend de** | `Granit.Core`, `Granit.Security` |
| **Utilisé par** | `Granit.BackgroundJobs`, `Granit.Webhooks`, `Granit.Wolverine.Postgresql`, `Granit.Persistence.Migrations` |
| **Package PostgreSQL** | `Granit.Wolverine.Postgresql` → ajoute `Granit.Persistence` |

> Voir le [graphe de dépendances complet](../dependencies.md).
