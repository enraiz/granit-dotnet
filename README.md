<p align="center">
  <img src="docs/images/granit-logo.svg" alt="granit" width="200" />
</p>

<p align="center">
  <strong>Framework .NET modulaire pour applications métier souveraines</strong>
</p>

<p align="center">
  .NET 10 · C# 14 · EF Core 10 · PostgreSQL · Keycloak · Vault · Serilog · OpenTelemetry · WolverineFx
</p>

---

Granit est le socle technique partagé des applications .NET de Digital Dynamics.
Il regroupe **41 packages NuGet** organisés en modules indépendants, conçus pour
un hébergement **souverain européen** (OVHcloud, Roubaix) et conformes aux
exigences **HDS** et **RGPD**.

## Fonctionnalités

| Domaine | Ce que Granit apporte |
| --- | --- |
| **Modularité** | Système de modules auto-configurés, tri topologique des dépendances |
| **Sécurité** | JWT Keycloak, RBAC, chiffrement Transit Vault, credentials dynamiques |
| **Persistance** | Intercepteurs EF Core : audit trail HDS 3 ans, soft delete RGPD, multi-tenancy |
| **Multi-tenancy** | Isolation par schéma ou par base, résolution automatique, filtrage transparent |
| **Observabilité** | Serilog + OpenTelemetry → OTLP (Loki, Tempo, Mimir), health checks, métriques |
| **Messaging** | Outbox transactionnelle WolverineFx, webhooks HMAC-SHA256, jobs cron |
| **API** | Versioning, OpenAPI Scalar, idempotence Stripe-style, ProblemDetails |
| **Stockage** | Blob storage S3 souverain, URL pré-signées, Crypto-Shredding |
| **SaaS** | Feature flags par plan commercial, quotas, résolution Default → Plan → Tenant |
| **Qualité** | Analyseurs Roslyn embarqués, validation FluentValidation (TVA, SIREN, NISS) |

## Démarrage rapide

```bash
# Ajouter le package fondation à votre projet
dotnet add package Granit.Core

# Ajouter les modules nécessaires
dotnet add package Granit.Persistence
dotnet add package Granit.Security
dotnet add package Granit.Observability
```

```csharp
// Program.cs
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await builder.AddGranitAsync<MyAppModule>();

WebApplication app = builder.Build();
app.Run();
```

```csharp
// MyAppModule.cs
[DependsOn(
    typeof(GranitPersistenceModule),
    typeof(GranitSecurityModule),
    typeof(GranitObservabilityModule))]
public sealed class MyAppModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Les modules Granit sont déjà configurés automatiquement.
        // Ajoutez ici la configuration spécifique à votre application.
    }
}
```

## Documentation

| Section | Contenu |
| --- | --- |
| [Framework](docs/framework/index.md) | Architecture, modules, sécurité, données, API, messaging, stockage |
| [Tests](docs/testing/index.md) | Conventions xUnit, mocking, assertions, intégration EF Core |
| [Catalogue des packages](docs/index.md) | Liste complète des 41 packages avec leur rôle |

## Contribuer

Voir [CONTRIBUTING.md](CONTRIBUTING.md) pour les conventions et le workflow de contribution.

## Changelog

Les changements sont documentés dans [CHANGELOG.md](CHANGELOG.md).

## Licence

Propriétaire — Digital Dynamics. Tous droits réservés. Voir [LICENSE](LICENSE).
