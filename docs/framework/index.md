# Granit — Documentation framework

Cette documentation est organisée en huit sections thématiques.

## Core

Architecture de base et configuration du framework au démarrage.

| Document | Description |
| --- | --- |
| [core.md](core/core.md) | Vue d'ensemble, module system, types domaine partagés |
| [modularity.md](core/modularity.md) | Système de modules, dépendances, lifecycle |
| [configuration.md](core/configuration.md) | Sources de configuration, IOptions\<T\>, secrets Vault |
| [options.md](core/options.md) | Options pattern, IOptionsMonitor, validation au démarrage |
| [settings.md](core/settings.md) | Paramètres dynamiques par tenant |

## Data

Modélisation, accès, isolation et performance de la donnée.

| Document | Description |
| --- | --- |
| [domain.md](data/domain.md) | Hiérarchie d'entités, ISoftDeletable, IMultiTenant, IActive |
| [persistence.md](data/persistence.md) | EF Core interceptors, audit trail HDS, soft delete RGPD |
| [data-filtering.md](data/data-filtering.md) | IDataFilter, bypass des query filters globaux |
| [caching.md](data/caching.md) | Cache distribué, invalidation, chiffrement des valeurs |
| [multi-tenancy.md](data/multi-tenancy.md) | Isolation par tenant, résolution, filtrage automatique |

## Security

Identité, droits d'accès et protection des secrets.

| Document | Description |
| --- | --- |
| [authentication.md](security/authentication.md) | JWT Bearer, intégration Keycloak |
| [authorization.md](security/authorization.md) | Policies RBAC, ICurrentUserService |
| [encryption.md](security/encryption.md) | Chiffrement Transit via Vault |
| [vault.md](security/vault.md) | VaultSharp, credentials dynamiques PostgreSQL, leases |

## Diagnostics

Surveillance, débogage et exposition du comportement de l'application.

| Document | Description |
| --- | --- |
| [logging.md](diagnostics/logging.md) | Serilog, enrichisseurs, bonnes pratiques HDS |
| [observability.md](diagnostics/observability.md) | OpenTelemetry, OTLP, Loki/Tempo/Mimir |
| [diagnostics.md](diagnostics/diagnostics.md) | Health checks, métriques, ActivitySource |
| [exception-handling.md](diagnostics/exception-handling.md) | Gestion des erreurs, ProblemDetails |

## API

Contrat HTTP : versioning des routes et documentation OpenAPI.

| Document | Description |
| --- | --- |
| [api-versioning.md](api/api-versioning.md) | Versioning HTTP par URL et query string |
| [api-documentation.md](api/api-documentation.md) | OpenAPI natif .NET 10, UI Scalar multi-version |
| [idempotency.md](api/idempotency.md) | Idempotence HTTP style Stripe, Redis SET NX PX, conformité HDS |

## Messaging

Messagerie asynchrone et outbox transactionnelle.

| Document | Description |
| --- | --- |
| [wolverine.md](messaging/wolverine.md) | WolverineFx, Outbox PostgreSQL, propagation de contexte HDS |

## Scheduling

Planification et exécution des tâches de fond récurrentes.

| Document | Description |
| --- | --- |
| [background-jobs.md](scheduling/background-jobs.md) | Jobs récurrents cron via Outbox Wolverine, `IBackgroundJobManager`, anti-doublon |

## Utilities

Briques utilitaires transverses.

| Document | Description |
| --- | --- |
| [timing.md](utilities/timing.md) | IClock, ICurrentTimezoneProvider, FakeTimeProvider |
| [guids.md](utilities/guids.md) | IGuidGenerator, GUID séquentiels pour index clustered |
| [localization/index.md](utilities/localization/index.md) | Localisation, ressources multilingues, endpoint HTTP SPA |
