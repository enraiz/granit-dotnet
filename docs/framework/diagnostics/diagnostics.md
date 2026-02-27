# Diagnostics

`Granit.Diagnostics` fournit l'infrastructure de health checks
production-ready pour les applications Digital Dynamics déployées sur Kubernetes :
exposition des trois sondes (`/health/live`, `/health/ready`, `/health/startup`),
cache anti-stampede et format de réponse JSON structuré pour l'observabilité.

## Installation

```bash
dotnet add package Granit.Diagnostics
```

## La trinité Kubernetes

Kubernetes utilise trois types de sondes distincts. Confondre leurs rôles provoque
des incidents en production.

| Sonde | Endpoint | Question posée | Action K8s si échec |
| ----- | -------- | -------------- | ------------------- |
| **Liveness** | `/health/live` | Le process est-il bloqué (deadlock) ? | Tue et redémarre le pod |
| **Readiness** | `/health/ready` | L'application peut-elle servir du trafic ? | Retire le pod du load balancer |
| **Startup** | `/health/startup` | L'initialisation lente est-elle terminée ? | Désactive liveness et readiness |

> **Règle d'or** : ne jamais vérifier une dépendance externe (base de données, Vault,
> Redis) dans la sonde liveness. Si la base a une latence élevée, K8s redémarre tous
> les pods simultanément, surcharge la base au redémarrage et provoque une panne totale.

## Configuration

### Avec le système de modules (recommandé)

```csharp
// Program.cs
builder.AddGranit<AppModule>();
// ...
app.UseGranit();
app.MapGranitHealthChecks();
```

```csharp
// AppModule.cs
[DependsOn(typeof(GranitDiagnosticsModule))]
public sealed class AppModule : GranitModule { }
```

### Enregistrement direct

```csharp
builder.Services.AddGranitDiagnostics();
// ...
app.MapGranitHealthChecks();
```

La configuration se fait via `appsettings.json` :

```json
{
  "Granit": {
    "Diagnostics": {
      "DefaultCacheDuration": "00:00:10"
    }
  }
}
```

### Personnalisation des chemins

```csharp
app.MapGranitHealthChecks(options =>
{
    options.LivenessPath  = "/health/live";   // défaut
    options.ReadinessPath = "/health/ready";  // défaut
    options.StartupPath   = "/health/startup"; // défaut
});
```

## Comportement des codes HTTP

| Statut | Liveness | Readiness | Startup |
| ------ | -------- | --------- | ------- |
| `Healthy` | 200 | 200 | 200 |
| `Degraded` | 200 | **200** | 200 |
| `Unhealthy` | 503 | **503** | 503 |

La sonde readiness retourne `200` en cas de `Degraded` : un service dégradé (ex. API
SMS externe lente) ne doit pas retirer le pod du load balancer pour les fonctions
principales. K8s ne connaît que deux états — le niveau de dégradation est visible dans
le payload JSON pour les alertes Grafana.

## Cache anti-stampede

Avec 50 pods et une sonde readiness toutes les 3 secondes, sans cache :
50 / 3 ≈ **16 requêtes par seconde** en continu vers la base de données.

Le décorateur `CachedHealthCheck` met les résultats en cache avec un `SemaphoreSlim`
(double-check locking) : si le cache expire simultanément pour plusieurs threads, un
seul exécute le check pendant que les autres attendent son résultat.

Durée de cache configurable via `DiagnosticsOptions` (défaut : **10 secondes**) dans
`appsettings.json` :

```json
{
  "Granit": {
    "Diagnostics": {
      "DefaultCacheDuration": "00:00:10"
    }
  }
}
```

Le décorateur est appliqué automatiquement à tous les checks enregistrés via les
extensions Granit (`AddGranitDbContextCheck`, `AddGranitVaultCheck`,
`AddGranitRedisCheck`).

## Format de réponse JSON

Les endpoints retournent un payload structuré, utile pour les alertes et dashboards
(Grafana, Loki). Kubernetes ne regarde que le code HTTP.

```json
{
  "status": "Healthy",
  "duration": 12.4,
  "checks": [
    {
      "name": "AppDbContext",
      "status": "Healthy",
      "duration": 8.1,
      "tags": ["readiness"]
    },
    {
      "name": "vault",
      "status": "Healthy",
      "duration": 4.3,
      "tags": ["readiness"]
    }
  ]
}
```

> **Conformité HDS/RGPD** : les réponses ne contiennent jamais de stack trace, de
> chaîne de connexion, de token ni de donnée médicale ou personnelle.

## Health checks par module

Chaque module Granit expose son propre check, opt-in, taggué `readiness`.

### Persistence (EF Core)

Utilise le package officiel Microsoft
`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.

```bash
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
```

```csharp
builder.Services
    .AddHealthChecks()
    .AddGranitDbContextCheck<AppDbContext>();
```

Le check exécute `CanConnectAsync()` sur le `DbContext`. Timeout configurable
(défaut : 5 secondes).

### Vault

```csharp
builder.Services
    .AddHealthChecks()
    .AddGranitVaultCheck();
```

Appelle `sys/health` via `IVaultClient` (VaultSharp). Les trois états possibles :

| État Vault | Résultat | Code HTTP readiness |
| ---------- | -------- | ------------------- |
| Actif | `Healthy` | 200 |
| Standby (réplication passive) | `Degraded` | 200 |
| Scellé ou inaccessible | `Unhealthy` | 503 |

### Redis

```csharp
builder.Services
    .AddHealthChecks()
    .AddGranitRedisCheck();
```

Effectue un `PING` via `IConnectionMultiplexer`. La latence est mesurée :

| Latence | Résultat | Code HTTP readiness |
| ------- | -------- | ------------------- |
| < 100 ms | `Healthy` | 200 |
| ≥ 100 ms | `Degraded` | 200 |
| Inaccessible | `Unhealthy` | 503 |

Seuil configurable via `AddGranitRedisCheck(degradedThreshold: TimeSpan.FromMilliseconds(100))`.

## Configuration Kubernetes

Exemple de manifeste pour un déploiement avec les trois sondes :

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 3
  failureThreshold: 3

startupProbe:
  httpGet:
    path: /health/startup
    port: 8080
  failureThreshold: 30
  periodSeconds: 5
```

> Avec `failureThreshold: 30` et `periodSeconds: 5`, la startup probe laisse jusqu'à
> 150 secondes à l'application pour terminer son initialisation (chargement de Vault,
> réchauffage des caches) avant que liveness et readiness ne soient activées.

## Observabilité

Le module `Granit.Observability` exclut automatiquement les endpoints `/health/*`
des traces OpenTelemetry (voir [observability.md](observability.md)). Avec 50 pods
sondés toutes les 3 secondes, l'exclusion économise **~86 400 spans inutiles par jour
par pod** dans Tempo.

## Architecture

```text
Granit.Diagnostics
├── Caching/
│   └── CachedHealthCheck.cs          (SemaphoreSlim + double-check locking)
├── ResponseWriters/
│   └── GranitHealthCheckWriter.cs (JSON structuré, sans PII)
├── Extensions/
│   ├── DiagnosticsServiceCollectionExtensions.cs  (AddGranitDiagnostics)
│   └── DiagnosticsEndpointRouteBuilderExtensions.cs (MapGranitHealthChecks)
├── DiagnosticsOptions.cs
└── GranitDiagnosticsModule.cs
```

## Conformité

| Exigence | Mécanisme |
| -------- | --------- |
| HDS - Pas d'exposition de données médicales | Réponses JSON sans PII ni stack trace |
| HDS - Traçabilité | Endpoints exclus des traces OTEL (pas de pollution du journal d'audit) |
| RGPD - Minimisation | Aucune donnée personnelle dans les payloads de santé |
| Souveraineté OVHcloud | Pas de dépendance US Cloud Act — packages Microsoft officiels uniquement |
| Résilience K8s | Liveness sans dépendances externes — jamais de restart en cascade |
| Anti-DDoS interne | Cache 10 s — 1 req/10 s par pod au lieu de ~16 req/s sans cache |

## Dépendances Granit

| Direction | Modules |
|-----------|---------|
| **Dépend de** | `Granit.Core` |
| **Utilisé par** | Module feuille (consommé par les applications) |

> Voir le [graphe de dépendances complet](../dependencies.md).
