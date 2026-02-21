# Observability

`DigitalDynamics.Foundation.Observability` configure Serilog (logs structurés) et
OpenTelemetry (traces + métriques) avec export OTLP vers la stack LGTM
(Loki/Grafana/Tempo/Mimir).

## Installation

```bash
dotnet add package DigitalDynamics.Foundation.Observability
```

## Configuration

### appsettings.json

```json
{
  "Observability": {
    "ServiceName": "guava-backend",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://otel-collector:4317",
    "ServiceNamespace": "guava-health",
    "Environment": "production",
    "EnableTracing": true,
    "EnableMetrics": true
  }
}
```

### Program.cs

Avec le système de modules (recommandé), `FoundationObservabilityModule` est chargé
automatiquement via `AddFoundation<T>()` (voir [modularity.md](modularity.md)).

Pour un enregistrement direct :

```csharp
builder.AddFoundationObservability();
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
    public string ServiceNamespace { get; set; } = "guava-health";
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
[10:30:45 INF] Guava.Modules.Auth.Handlers.SyncUserProfileHandler Profil synchronisé
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
DigitalDynamics.Foundation.Observability
├── Options/
│   └── ObservabilityOptions.cs
├── FoundationObservabilityModule.cs        (module Foundation)
└── Extensions/
    └── ObservabilityServiceCollectionExtensions.cs  (AddFoundationObservability)
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
