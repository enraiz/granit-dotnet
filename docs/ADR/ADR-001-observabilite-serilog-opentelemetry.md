# ADR-001 : Stack d'observabilité — Serilog + OpenTelemetry

- **Statut** : Accepté
- **Date** : 2026-02-21
- **Issue** : [#10](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/10)
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.Observability)

## Contexte

Le framework Granit fournit le module `Granit.Observability` qui encapsule la
configuration du logging structuré, du tracing distribué et des métriques. Le
choix des bibliothèques d'instrumentation conditionne :

- **Traçabilité ISO 27001** : logs structurés horodatés conservés 3 ans
- **Tracing distribué** : corrélation des requêtes à travers les modules,
  messages Wolverine et appels HTTP
- **Métriques** : supervision des performances et alerting
- **Souveraineté** : aucune donnée de télémétrie ne doit quitter l'infrastructure
  européenne (OVHcloud)

Les données d'observabilité sont exportées via le protocole OTLP vers une stack
Grafana self-hosted : **Loki** (logs), **Tempo** (traces), **Mimir** (métriques).

## Décision

- **Logging** : Serilog (`Serilog.AspNetCore` + `Serilog.Sinks.OpenTelemetry`)
- **Tracing & métriques** : OpenTelemetry SDK .NET (7 packages)
- **Export** : OTLP (OpenTelemetry Protocol) vers la stack Grafana

## Alternatives évaluées

### Option 1 : Serilog + OpenTelemetry (retenue)

- **Logging** : Serilog — logging structuré, enrichisseurs (contexte, tenant, user),
  sink OTLP pour unifier le pipeline
- **Tracing** : OpenTelemetry — standard CNCF, instrumentation automatique
  (ASP.NET Core, HTTP, EF Core), propagation W3C Trace Context
- **Export** : OTLP vers Loki/Tempo/Mimir (self-hosted OVHcloud)

### Option 2 : Microsoft.Extensions.Logging + OpenTelemetry seul

- **Avantage** : pas de dépendance tierce pour le logging
- **Inconvénient** : enrichissement limité, pas de sink dédié pour les formats
  structurés avancés, configuration moins flexible que Serilog
  (filtrage par namespace, destructuring)

### Option 3 : NLog + OpenTelemetry

- **Avantage** : NLog est mature et performant
- **Inconvénient** : écosystème de sinks OTLP moins développé que Serilog,
  configuration XML (vs fluent API), communauté .NET moderne orientée Serilog

### Option 4 : Application Insights (Azure Monitor)

- **Avantage** : intégration native .NET, dashboard prêt à l'emploi
- **Inconvénient** : **incompatible souveraineté** — données hébergées sur Azure
  (Cloud Act US), coût variable basé sur le volume d'ingestion, vendor lock-in

### Option 5 : Datadog / New Relic

- **Avantage** : solution SaaS complète (logs + traces + métriques + APM)
- **Inconvénient** : **incompatible souveraineté** — données hors UE (ou région UE
  mais entreprise US soumise au Cloud Act), coût élevé par host/GB

## Justification

| Critère | Serilog + OTel | MEL + OTel | NLog + OTel | App Insights | Datadog |
| ------- | -------------- | ---------- | ----------- | ------------ | ------- |
| Souveraineté | Self-hosted | Self-hosted | Self-hosted | Non (Azure) | Non (US) |
| Standard CNCF | Oui (OTel) | Oui (OTel) | Oui (OTel) | Partiel | Partiel |
| Enrichissement logs | Excellent | Basique | Bon | Bon | Excellent |
| Sink OTLP natif | Oui | Non | Partiel | N/A | N/A |
| Communauté .NET | Très large | Standard | Moyenne | Large | Moyenne |
| Coût | Infra seule | Infra seule | Infra seule | Variable | Élevé |
| Conformité ISO 27001 | Oui | Oui | Oui | Risque | Risque |

## Packages utilisés

| Package | Rôle |
| ------- | ---- |
| `Serilog.AspNetCore` | Intégration ASP.NET Core, enrichisseurs de requête |
| `Serilog.Sinks.OpenTelemetry` | Export des logs via OTLP |
| `OpenTelemetry` | SDK core |
| `OpenTelemetry.Api` | API d'instrumentation (ActivitySource, Meter) |
| `OpenTelemetry.Extensions.Hosting` | Intégration `IHostBuilder` |
| `OpenTelemetry.Instrumentation.AspNetCore` | Traces automatiques des requêtes HTTP |
| `OpenTelemetry.Instrumentation.Http` | Traces automatiques des appels `HttpClient` |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | Traces automatiques des requêtes EF Core |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | Export OTLP vers Loki/Tempo/Mimir |

## Conséquences

### Positives

- Conformité souveraineté : zéro donnée de télémétrie hors OVHcloud
- Standard CNCF : portabilité vers tout backend compatible OTLP
- Corrélation complète : logs ↔ traces ↔ métriques via le même TraceId
- Enrichissement contextuel Serilog : tenant, user, module, correlation-id
- Dashboard Grafana unifié pour toute la plateforme

### Négatives

- Maintenance de la stack Grafana (Loki, Tempo, Mimir) à la charge de l'équipe SRE
- Configuration initiale plus complexe qu'une solution SaaS
- Serilog est une dépendance tierce (risque de maintenance, bien que très stable)

## Conditions de réévaluation

Ce choix devrait être réévalué si :

- Un service d'observabilité managé souverain européen émerge (certifié ISO 27001)
- OpenTelemetry .NET SDK atteint la parité fonctionnelle avec Serilog pour le logging structuré
- La charge de maintenance de la stack Grafana self-hosted devient disproportionnée

## Références

- Commit initial : `52f1444` (2026-02-21)
- Issues : [#10 — Granit.Observability](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/10), [#222](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/222)
- Serilog : <https://serilog.net/>
- OpenTelemetry .NET : <https://opentelemetry.io/docs/languages/dotnet/>
