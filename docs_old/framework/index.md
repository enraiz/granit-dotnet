<p align="center">
  <img src="../../docs-site/src/assets/granit-icon.svg" alt="granit" width="160" />
</p>

# Granit — Documentation framework

Cette documentation est organisée en dix sections thématiques.

## Core

Architecture de base et configuration du framework au démarrage.

| Document | Description |
| --- | --- |
| [core.md](core/core.md) | Vue d'ensemble, module system, types domaine partagés |
| [modularity.md](core/modularity.md) | Système de modules, dépendances, lifecycle |
| [configuration/](core/configuration/index.md) | Configuration, options, settings, module config — arbre de décision |

## Data

Modélisation, accès, isolation et performance de la donnée.

| Document | Description |
| --- | --- |
| [domain.md](data/domain.md) | Hiérarchie d'entités, ISoftDeletable, IMultiTenant, IActive |
| [persistence.md](data/persistence.md) | EF Core interceptors, audit trail ISO 27001, soft delete RGPD, data seeding |
| [data-filtering.md](data/data-filtering.md) | IDataFilter, bypass des query filters globaux |
| [caching.md](data/caching.md) | Cache distribué, invalidation, chiffrement des valeurs |
| [multi-tenancy.md](data/multi-tenancy.md) | Isolation par tenant, résolution, filtrage automatique |
| [translations.md](data/translations.md) | Entités traduisibles, résolution multilingue, conventions EF Core |

## Security

Identité, droits d'accès et protection des secrets.

| Document | Description |
| --- | --- |
| [authentication.md](security/authentication.md) | JWT Bearer, intégration Keycloak |
| [authorization.md](security/authorization.md) | Policies RBAC, ICurrentUserService |
| [identity.md](security/identity.md) | `IIdentityProvider` — users, sessions, device activity, enable/disable, mot de passe |
| [encryption.md](security/encryption.md) | Chiffrement Transit via Vault |
| [vault.md](security/vault.md) | VaultSharp, credentials dynamiques PostgreSQL, leases |
| [cors.md](security/cors.md) | Configuration CORS standardisée, validation ISO 27001 au démarrage |

## Diagnostics

Surveillance, débogage et exposition du comportement de l'application.

| Document | Description |
| --- | --- |
| [logging.md](diagnostics/logging.md) | Serilog, enrichisseurs, bonnes pratiques ISO 27001 |
| [observability.md](diagnostics/observability.md) | OpenTelemetry, OTLP, Loki/Tempo/Mimir |
| [wolverine-tracing.md](diagnostics/wolverine-tracing.md) | Traçage distribué Wolverine — propagation W3C Trace Context dans l'Outbox |
| [diagnostics.md](diagnostics/diagnostics.md) | Health checks, métriques, ActivitySource |
| [exception-handling.md](diagnostics/exception-handling.md) | Gestion des erreurs, ProblemDetails |
| [analyzers.md](diagnostics/analyzers.md) | Analyseurs Roslyn — migrations, sécurité, conventions EF Core |

## API

Contrat HTTP : versioning des routes et documentation OpenAPI.

| Document | Description |
| --- | --- |
| [api-versioning.md](api/api-versioning.md) | Versioning HTTP par URL et query string |
| [api-documentation.md](api/api-documentation.md) | OpenAPI natif .NET 10, UI Scalar multi-version |
| [http-responses.md](api/http-responses.md) | Codes de retour HTTP — conventions 200/201/202/204, pattern asynchrone, ISO 27001 |
| [idempotency.md](api/idempotency.md) | Idempotence HTTP style Stripe, Redis SET NX PX, conformité ISO 27001 |

## Messaging

Messagerie asynchrone et outbox transactionnelle.

| Document | Description |
| --- | --- |
| [wolverine.md](messaging/wolverine.md) | WolverineFx, Outbox PostgreSQL, propagation de contexte ISO 27001 |
| [webhooks.md](messaging/webhooks.md) | Webhooks sortants, fan-out Wolverine, HMAC-SHA256, audit trail ISO 27001 |
| [notifications.md](messaging/notifications.md) | Notifications multi-canal (InApp, SignalR, Email, SMS, WhatsApp, Push), fan-out Wolverine, entity tracking Odoo-style |

## Scheduling

Planification et exécution des tâches de fond récurrentes.

| Document | Description |
| --- | --- |
| [background-jobs.md](scheduling/background-jobs.md) | Jobs récurrents cron via Outbox Wolverine, `IBackgroundJobManager`, anti-doublon |

## Templating

Rendu de templates et génération documentaire (PDF, Excel).

| Document | Description |
| --- | --- |
| [templating/index.md](templating/index.md) | Pipeline Scriban, `IDocumentGenerator`, enrichisseurs, cycle de vie Draft/Published/Deprecated |

## Imaging

Manipulation d'images : redimensionnement, recadrage, compression, conversion et watermark.

| Document | Description |
| --- | --- |
| [imaging/index.md](imaging/index.md) | API fluide `IImageProcessor` / `IImagePipeline`, Magick.NET, formats WebP/AVIF, strip EXIF (RGPD) |

## Storage

Stockage d'objets souverain, Direct-to-Cloud, conforme ISO 27001 et RGPD.

| Document | Description |
| --- | --- |
| [blob-storage.md](storage/blob-storage.md) | Stockage de fichiers S3, URL pré-signées, pipeline de validation, Crypto-Shredding |

## SaaS

Gestion des fonctionnalités par plan commercial et quotas.

| Document | Description |
| --- | --- |
| [features.md](saas/features.md) | Feature Management (Toggle/Numeric/Selection), résolution Default → Plan → Tenant, cache hybride, `[RequiresFeature]`, `IFeatureLimitGuard` |

## Utilities

Briques utilitaires transverses.

| Document | Description |
| --- | --- |
| [timing.md](utilities/timing.md) | IClock, ICurrentTimezoneProvider, FakeTimeProvider |
| [guids.md](utilities/guids.md) | IGuidGenerator, GUID séquentiels pour index clustered |
| [validation/index.md](utilities/validation/index.md) | Validation FluentValidation : identifiants légaux BE/FR, TVA UE, paiements, contacts |
| [localization/index.md](utilities/localization/index.md) | Localisation, ressources multilingues, endpoint HTTP SPA |

## Transversal

| Document | Description |
| --- | --- |
| [dependencies.md](dependencies.md) | Graphe de dépendances inter-modules (Mermaid) |
