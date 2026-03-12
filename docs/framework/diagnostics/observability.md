# Observability

`Granit.Observability` configure Serilog (logs structurés) et
OpenTelemetry (traces + métriques) avec export OTLP vers la stack LGTM
(Loki/Grafana/Tempo/Mimir).

## Installation

```bash
dotnet add package Granit.Observability
```

## Configuration

### appsettings.json

```json
{
  "Observability": {
    "ServiceName": "my-backend",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://otel-collector:4317",
    "ServiceNamespace": "my-company",
    "Environment": "production",
    "EnableTracing": true,
    "EnableMetrics": true
  }
}
```

### Program.cs

Avec le système de modules (recommandé), `GranitObservabilityModule` est chargé
automatiquement via `AddGranit<T>()` (voir [modularity.md](../core/modularity.md)).

Pour un enregistrement direct :

```csharp
builder.AddGranitObservability();
```

Cette méthode configure automatiquement Serilog et OpenTelemetry.

## ObservabilityOptions

```csharp
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "unknown-service";
    public string ServiceVersion { get; set; } = "0.0.0";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public string ServiceNamespace { get; set; } = "my-company";
    public string Environment { get; set; } = "development";
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
}
```

## Serilog

Serilog est configuré avec :

- **Console sink** : format structuré pour le développement local
- **OpenTelemetry sink** : export vers le collecteur OTLP (→ Loki)
- **Enrichissement** : `ServiceName`, `ServiceVersion`, `Environment`, `LogContext`

Format console :

```text
[10:30:45 INF] MyApp.Modules.Auth.Handlers.SyncUserProfileHandler Profil synchronisé
```

### Configuration avancée

Serilog lit aussi depuis `appsettings.json` via `ReadFrom.Configuration()`. Exemple
pour ajuster le niveau minimum :

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

## OpenTelemetry

### Traces

Instrumentations activées automatiquement :

| Instrumentation | Données collectées |
| --- | --- |
| ASP.NET Core | Requêtes HTTP entrantes (sauf `/healthz`) |
| HttpClient | Requêtes HTTP sortantes |
| Entity Framework Core | Requêtes SQL |

Les exceptions sont enregistrées automatiquement (`RecordException = true`).

Le endpoint `/healthz` est exclu des traces pour éviter le bruit.

### Métriques

Instrumentations activées automatiquement :

| Instrumentation | Métriques collectées |
| --- | --- |
| ASP.NET Core | Durée des requêtes, codes de réponse |
| HttpClient | Durée des appels sortants |

### Export OTLP

Traces et métriques sont exportés via gRPC vers le collecteur OpenTelemetry
configuré dans `OtlpEndpoint`.

Pipeline typique en production :

```text
Application → OTLP gRPC → OpenTelemetry Collector → Tempo (traces)
                                                   → Mimir (métriques)
                                                   → Loki (logs)
```

### Désactivation

Les traces et métriques peuvent être désactivées individuellement :

```json
{
  "Observability": {
    "EnableTracing": false,
    "EnableMetrics": false
  }
}
```

## Architecture

```text
Granit.Observability
├── Options/
│   └── ObservabilityOptions.cs
├── GranitObservabilityModule.cs        (module Granit)
└── Extensions/
    └── ObservabilityServiceCollectionExtensions.cs  (AddGranitObservability)
```

## Resource attributes

Les attributs de ressource OpenTelemetry sont configurés automatiquement :

| Attribut | Source |
| --- | --- |
| `service.name` | `ObservabilityOptions.ServiceName` |
| `service.version` | `ObservabilityOptions.ServiceVersion` |
| `service.namespace` | `ObservabilityOptions.ServiceNamespace` |
| `deployment.environment` | `ObservabilityOptions.Environment` |

## Dépendances

| Package | Rôle |
| --- | --- |
| `Serilog.AspNetCore` | Intégration Serilog avec ASP.NET Core |
| `Serilog.Sinks.OpenTelemetry` | Export des logs via OTLP |
| `OpenTelemetry.Extensions.Hosting` | Intégration OTEL avec le host .NET |
| `OpenTelemetry.Instrumentation.AspNetCore` | Instrumentation automatique ASP.NET |
| `OpenTelemetry.Instrumentation.Http` | Instrumentation automatique HttpClient |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | Instrumentation automatique EF Core |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | Export gRPC vers le collecteur |

## Tracing distribué des modules Granit

### GranitActivitySourceRegistry

Chaque module Granit qui effectue de l'I/O significatif déclare un
`ActivitySource` dédié et l'enregistre via `GranitActivitySourceRegistry.Register()`
dans son `AddGranit*()`. `AddGranitObservability()` itère le registre et appelle
`.AddSource()` pour chaque entrée.

**Pattern soft dependency** : aucun module ne référence `Granit.Observability`.
Si aucun listener n'est enregistré, `StartActivity()` retourne `null` (no-op).

### ActivitySources enregistrés

| Source | Module | Spans |
| --- | --- | --- |
| `Granit.Wolverine` | Granit.Wolverine | `wolverine.message.handle` |
| `Granit.Webhooks` | Granit.Webhooks | `webhooks.deliver`, `webhooks.fanout` |
| `Granit.Notifications` | Granit.Notifications | `notifications.deliver`, `notifications.fanout` |
| `Granit.BackgroundJobs` | Granit.BackgroundJobs | `backgroundjobs.trigger` |
| `Granit.BlobStorage.S3` | Granit.BlobStorage.S3 | `blobstorage.upload-ticket`, `blobstorage.download-url`, `blobstorage.delete`, `blobstorage.get-size`, `blobstorage.partial-stream` |
| `Granit.Identity.Keycloak` | Granit.Identity.Keycloak | `identity.keycloak.*` (toutes les méthodes publiques IIdentityProvider + token-acquire) |
| `Granit.Identity.EntraId` | Granit.Identity.EntraId | `identity.entraid.*` (toutes les méthodes publiques IIdentityProvider + token-acquire) |

### Ajouter le tracing à un nouveau module

1. Créer `Diagnostics/<Module>ActivitySource.cs` :

    ```csharp
    internal static class MyModuleActivitySource
    {
        internal const string Name = "Granit.MyModule";
        internal static readonly ActivitySource Source = new(Name);
        internal const string MyOperation = "mymodule.my-operation";
    }
    ```

2. Enregistrer dans `AddGranitMyModule()` :

    ```csharp
    GranitActivitySourceRegistry.Register(MyModuleActivitySource.Name);
    ```

3. Instrumenter les points d'I/O :

    ```csharp
    using var activity = MyModuleActivitySource.Source.StartActivity(MyModuleActivitySource.MyOperation);
    activity?.SetTag("mymodule.key", value);
    ```

### Contrainte d'ordre

`AddGranitObservability()` doit être appelé **après** les modules (c'est le cas
par défaut dans le pattern Granit — observabilité = dernière étape de configuration).

### Intégration Wolverine

Lorsque `Granit.Wolverine` est installé, la source `Granit.Wolverine` est
enregistrée automatiquement. Les **spans bridge** créés par `TraceContextBehavior`
lient les traitements Outbox asynchrones à la requête HTTP d'origine sous le
**même `trace-id`** dans Grafana/Tempo.

→ Voir [wolverine-tracing.md](wolverine-tracing.md) pour le détail.

## Dépendances Granit

| Direction       | Modules                                        |
|-----------------|------------------------------------------------|
| **Dépend de**   | `Granit.Core`                                  |
| **Utilisé par** | Module feuille (consommé par les applications) |

> Voir le [graphe de dépendances complet](../dependencies.md).
