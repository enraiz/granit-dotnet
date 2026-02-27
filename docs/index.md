<p align="center">
  <img src="images/granit-logo.svg" alt="granit" width="160" />
</p>

# granit

Framework .NET partagé pour les applications Digital Dynamics.

Granit fournit un socle modulaire de packages NuGet couvrant les besoins
transversaux des applications métier : sécurité, persistance, observabilité,
multi-tenancy, chiffrement, messaging et plus encore.

Conçu pour un hébergement souverain (OVHcloud, Roubaix) et conforme aux
exigences **HDS** et **RGPD**.

## Stack technique

.NET 10 · C# 14 · EF Core 10 · PostgreSQL · Keycloak · HashiCorp Vault ·
Serilog · OpenTelemetry · WolverineFx

## Packages (41)

### Fondation

| Package | Rôle |
| --- | --- |
| `Granit.Core` | Système de modules, types domaine partagés, data filtering |
| `Granit.Timing` | `IClock`, `ICurrentTimezoneProvider`, `TimeProvider` |
| `Granit.Guids` | `IGuidGenerator`, GUID séquentiels pour index clustered |
| `Granit.Validation` | Validation FluentValidation (identifiants légaux, TVA, contacts) |
| `Granit.Localization` | Ressources multilingues, surcharges par tenant |
| `Granit.Localization.Endpoints` | Endpoint HTTP pour SPA |
| `Granit.Localization.EntityFrameworkCore` | Persistance EF Core des surcharges |
| `Granit.Analyzers` | Analyseurs Roslyn spécifiques Granit |

### Sécurité et identité

| Package | Rôle |
| --- | --- |
| `Granit.Security` | JWT Keycloak, `ICurrentUserService` |
| `Granit.Authentication.JwtBearer` | Authentification JWT Bearer |
| `Granit.Authentication.Keycloak` | Intégration Keycloak (discovery, rôles) |
| `Granit.Authorization` | Policies RBAC, permissions |
| `Granit.Authorization.EntityFrameworkCore` | Persistance EF Core des permissions |
| `Granit.Encryption` | Chiffrement Transit via Vault |
| `Granit.Vault` | VaultSharp, credentials dynamiques PostgreSQL, leases |

### Données et persistance

| Package | Rôle |
| --- | --- |
| `Granit.Persistence` | Intercepteurs EF Core (audit HDS, soft delete RGPD) |
| `Granit.Persistence.Migrations` | Infrastructure de migrations multi-tenant |
| `Granit.MultiTenancy` | Isolation par tenant, résolution, filtrage automatique |
| `Granit.Caching` | Abstractions de cache distribué |
| `Granit.Caching.Hybrid` | Cache hybride L1/L2 |
| `Granit.Caching.StackExchangeRedis` | Implémentation Redis |
| `Granit.Settings` | Paramètres dynamiques par tenant |
| `Granit.Settings.EntityFrameworkCore` | Persistance EF Core des paramètres |

### API et HTTP

| Package | Rôle |
| --- | --- |
| `Granit.ApiVersioning` | Versioning HTTP par URL et query string |
| `Granit.ApiDocumentation` | OpenAPI natif .NET 10, UI Scalar multi-version |
| `Granit.Idempotency` | Idempotence HTTP style Stripe, Redis `SET NX PX` |
| `Granit.ExceptionHandling` | Gestion des erreurs, `ProblemDetails` |

### Messaging et tâches

| Package | Rôle |
| --- | --- |
| `Granit.Wolverine` | WolverineFx, outbox transactionnelle, contexte HDS |
| `Granit.Wolverine.Postgresql` | Transport PostgreSQL pour Wolverine |
| `Granit.Webhooks` | Webhooks sortants, fan-out, HMAC-SHA256, audit HDS |
| `Granit.Webhooks.EntityFrameworkCore` | Persistance EF Core des webhooks |
| `Granit.BackgroundJobs` | Jobs récurrents cron via outbox Wolverine |
| `Granit.BackgroundJobs.Endpoints` | Endpoints HTTP d'administration des jobs |
| `Granit.BackgroundJobs.EntityFrameworkCore` | Persistance EF Core des jobs |

### Observabilité

| Package | Rôle |
| --- | --- |
| `Granit.Diagnostics` | Health checks, métriques, `ActivitySource` |
| `Granit.Observability` | Serilog + OpenTelemetry → OTLP → Loki/Tempo/Mimir |

### Stockage et SaaS

| Package | Rôle |
| --- | --- |
| `Granit.BlobStorage` | Abstractions stockage d'objets souverain |
| `Granit.BlobStorage.EntityFrameworkCore` | Persistance EF Core des métadonnées |
| `Granit.BlobStorage.S3` | Implémentation S3 (OVHcloud Object Storage) |
| `Granit.Features` | Feature Management (Toggle/Numeric/Selection), résolution par plan |
| `Granit.Features.EntityFrameworkCore` | Persistance EF Core des features |

## Documentation

| Section | Description |
| --- | --- |
| [Framework](framework/index.md) | Architecture, modules, sécurité, données, diagnostics, API, messaging, stockage |
| [Guide](guide/index.md) | Tutoriels pas-à-pas, démarrage rapide |
| [Cookbook](cookbook/index.md) | Recettes pratiques cross-modules |
| [Déploiement](deployment/index.md) | Observabilité, Vault, Kubernetes, checklist production |
| [Référence API](api/index.md) | Documentation C# générée par DocFX |
| [Patterns](patterns/index.md) | 39 design patterns identifiés dans Granit |
| [Tests](testing/index.md) | Conventions, mocking, assertions, intégration EF Core, conformité HDS/RGPD |

## Démarrage rapide

```bash
# Ajouter le package fondation
dotnet add package Granit.Core

# Build
dotnet build

# Tests
dotnet test

# Vérifier le formatage
dotnet format --verify-no-changes
```

## Conformité

| Norme | Couverture |
| --- | --- |
| **HDS** | Audit trail 3 ans, chiffrement at rest et in transit, traçabilité des accès |
| **RGPD** | Minimisation, droit à l'effacement (soft delete), pseudonymisation |
| **ISO 27001** | Gestion des secrets (Vault), rotation automatique, zéro secret en clair |
