# Traçage distribué — Granit.Wolverine

Ce document décrit la propagation du **W3C Trace Context** à travers les frontières
asynchrones de Wolverine (Outbox), et comment corréler visuellement une requête HTTP
et tous ses traitements asynchrones dans Grafana/Tempo.

## Le problème : la frontière de l'Outbox

Sans propagation de contexte, une requête HTTP entrante et les workers Wolverine qui
en découlent sont des traces **orphelines et déconnectées** dans Tempo :

```text
Requête HTTP ─── [trace-id: abc123]
                  ├── Span : POST /orders
                  └── Span : INSERT SQL (EF Core)

Worker Wolverine ─── [trace-id: xyz789]  ← trace totalement séparée !
                      ├── Span : Handle OrderCreatedEvent
                      └── Span : SELECT SQL (EF Core)
```

Il est impossible de retrouver le worker à partir de la requête initiale.

## La solution : TraceContextBehavior

`Granit.Wolverine` propagate automatiquement le `traceparent` W3C dans les enveloppes
de messages. À la réception, `TraceContextBehavior` restaure le contexte et crée une
**activity bridge** qui relie les deux traces dans Tempo :

```text
Requête HTTP ─── [trace-id: abc123]
                  ├── Span : POST /orders
                  ├── Span : INSERT SQL (EF Core)
                  └── Span : wolverine.message.handle  ← bridge
                              ├── Span : Handle OrderCreatedEvent
                              └── Span : SELECT SQL (EF Core)
```

En tapant `abc123` dans Grafana, toute la chaîne s'affiche.

## Fonctionnement

### Côté émetteur — OutgoingContextMiddleware

Lorsqu'un message est publié depuis un contexte HTTP actif, `OutgoingContextMiddleware`
injecte le `traceparent` courant dans l'enveloppe :

```text
Activity.Current.Id → envelope.Headers["traceparent"]
```

Exemple de valeur : `00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01`

Format W3C : `{version}-{traceId}-{parentId}-{flags}`

### Côté récepteur — TraceContextBehavior

Lorsque le worker Wolverine traite le message (secondes, minutes, ou heures plus tard) :

1. Lit le header `traceparent` de l'enveloppe
2. Parse le contexte avec `ActivityContext.TryParse()`
3. Démarre une activity bridge nommée `wolverine.message.handle` avec ce contexte comme
   parent — elle hérite du **même `trace-id`** que la requête HTTP d'origine
4. Tous les spans créés pendant l'exécution du handler (EF Core, HttpClient, etc.)
   deviennent enfants de cette activity bridge

Pipeline d'exécution complet :

```text
[Message entrant]
  → TenantContextBehavior.Before()    — restaure ICurrentTenant
  → UserContextBehavior.Before()      — restaure ICurrentUserService
  → TraceContextBehavior.Before()     — restaure le trace-id (Activity bridge)
  → [Handler]
  → TraceContextBehavior.After()      — dispose l'activity bridge
  → UserContextBehavior.After()
  → TenantContextBehavior.After()
```

### Tags OTel sur le span bridge

| Tag | Valeur |
| --- | --- |
| `messaging.system` | `wolverine` |
| `messaging.operation` | `process` |
| `messaging.message_id` | `envelope.Id` (GUID) |
| `messaging.message_type` | `envelope.MessageType` (si disponible) |

## Prérequis

### Granit.Observability

La source `Granit.Wolverine` doit être enregistrée dans le provider OTel pour que les
spans bridge soient exportés vers Tempo. `Granit.Observability` le fait automatiquement
via `AddSource("Granit.Wolverine")` dans `AddGranitObservability()`.

Aucune configuration supplémentaire n'est nécessaire si les deux packages sont utilisés.

### Sans Granit.Observability

Si vous configurez OpenTelemetry manuellement, ajoutez :

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Granit.Wolverine")
        // ... autres sources
    );
```

## Comportements de bord

| Situation | Comportement |
| --- | --- |
| Pas de header `traceparent` | No-op — le handler s'exécute normalement |
| Header `traceparent` malformé | Log `Warning` + no-op (pas d'exception) |
| Source `Granit.Wolverine` non écoutée | `StartActivity()` retourne `null` — no-op silencieux |
| Pas d'activité OTel active à l'émission | Header `traceparent` absent — voir ligne 1 |

## Conformité

Le `traceparent` est un **identifiant opaque** (UUID de trace + UUID de span).
Il ne contient aucune donnée de santé ni aucune donnée personnelle. Sa propagation
ne crée pas de risque RGPD ni HDS.

Les logs de Warning pour les headers malformés ne contiennent que l'ID de l'enveloppe
(GUID interne) et la valeur du header — jamais de PII.
