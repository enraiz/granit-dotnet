# Étape 7 — Observabilité

Granit intègre Serilog (logs structurés) et OpenTelemetry (traces distribuées,
métriques) avec export OTLP vers une stack LGTM souveraine
(Loki / Grafana / Tempo / Mimir).

## Ajouter le package

```bash
dotnet add package Granit.Observability
```

## Configuration

Ajouter la section dans `appsettings.Development.json` :

```json
{
  "Observability": {
    "ServiceName": "task-management-api",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

> En développement sans collector OTLP, les logs sont écrits sur la console.
> Le `OtlpEndpoint` est optionnel.

## Mettre à jour le module

`GranitObservabilityModule` utilise `IHostApplicationBuilder` (pas `IServiceCollection`).
Il est configuré automatiquement via `AddGranitAsync` :

```csharp
using Granit.Observability;

// Ajouter à la liste des [DependsOn]
[DependsOn(typeof(GranitObservabilityModule))]
public sealed class TaskManagementModule : GranitModule
{
    // ...
}
```

## Ce qui est enregistré automatiquement

| Composant | Description |
| --- | --- |
| **Serilog** | Remplace le logger par défaut, logs structurés JSON |
| **OpenTelemetry Traces** | Instrumentation HTTP, EF Core, Wolverine |
| **OpenTelemetry Metrics** | Métriques .NET runtime, ASP.NET Core, custom |
| **OTLP Exporter** | Export vers Loki (logs), Tempo (traces), Mimir (metrics) |
| **Enrichissement** | `ServiceName`, `Environment`, `TenantId`, `UserId` ajoutés à chaque log |

## Logs structurés

Utiliser `ILogger<T>` standard (pas d'abstraction Granit) :

```csharp
using Microsoft.Extensions.Logging;

internal static class TaskEndpoints
{
    private static async Task<Created<TaskItem>> CreateAsync(
        CreateTaskRequest request,
        TaskDbContext db,
        ILogger<TaskItem> logger,
        CancellationToken ct)
    {
        TaskItem task = new()
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Task {TaskId} created with title '{Title}'",
            task.Id, task.Title);

        return TypedResults.Created($"/api/tasks/{task.Id}", task);
    }
}
```

Le log produit en JSON structuré :

```json
{
  "Timestamp": "2026-02-27T10:30:00Z",
  "Level": "Information",
  "MessageTemplate": "Task {TaskId} created with title '{Title}'",
  "Properties": {
    "TaskId": "019...",
    "Title": "Lire la doc Granit",
    "ServiceName": "task-management-api",
    "UserId": "john.doe",
    "TraceId": "abc123...",
    "SpanId": "def456..."
  }
}
```

## Traces distribuées

Chaque requête HTTP génère une trace avec un `TraceId` unique.
Dans Grafana Tempo, on peut suivre la requête de bout en bout :

```text
HTTP GET /api/tasks  (12ms)
├── EF Core: SELECT * FROM tasks  (3ms)
└── Response: 200 OK
```

Le `TraceId` est inclus dans chaque log et dans le header de réponse HTTP,
permettant la corrélation entre logs et traces.

## Prochaine étape

L'application est observable. Terminons par les [tests](08-tests.md).

## Référence

- [Observabilité](../../framework/diagnostics/observability.md)
- [Logging](../../framework/diagnostics/logging.md)
- [Health checks](../../framework/diagnostics/diagnostics.md)
