<p align="center">
  <img src="docs/images/granit-logo.svg" alt="granit" width="200" />
</p>

<p align="center">
  <strong>Solid by design. Modular by nature.</strong>
</p>

<p align="center">
  .NET 10 · C# 14 · EF Core 10 · PostgreSQL · Keycloak · Vault · Serilog · OpenTelemetry · WolverineFx
</p>

---

Granit is a rock-solid, production-ready modular framework for .NET and React.
Built with Vertical Slicing and zero compromises on Developer Experience.
It provides **41 NuGet packages** organized as independent modules, designed for
**sovereign European hosting** (OVHcloud) and compliant with
**ISO 27001** and **GDPR** requirements.

## Features

| Domain | What Granit provides |
| --- | --- |
| **Modularity** | Self-configuring module system, topological dependency sorting |
| **Security** | JWT Keycloak, RBAC, Vault Transit encryption, dynamic credentials |
| **Persistence** | EF Core interceptors: ISO 27001 audit trail (3 years), GDPR soft delete, multi-tenancy |
| **Multi-tenancy** | Schema or database isolation, automatic resolution, transparent filtering |
| **Observability** | Serilog + OpenTelemetry → OTLP (Loki, Tempo, Mimir), health checks, metrics |
| **Messaging** | WolverineFx transactional outbox, HMAC-SHA256 webhooks, cron jobs |
| **API** | Versioning, OpenAPI Scalar, Stripe-style idempotency, ProblemDetails |
| **Storage** | Sovereign S3 blob storage, pre-signed URLs, Crypto-Shredding |
| **SaaS** | Feature flags per commercial plan, quotas, Default → Plan → Tenant resolution |
| **Quality** | Embedded Roslyn analyzers, FluentValidation (VAT, SIREN, NISS) |

## Quick start

```bash
# Add the foundation package to your project
dotnet add package Granit.Core

# Add the modules you need
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
        // Granit modules are already configured automatically.
        // Add your application-specific configuration here.
    }
}
```

## Documentation

| Section | Content |
| --- | --- |
| [Framework](docs/framework/index.md) | Architecture, modules, security, data, API, messaging, storage |
| [Tests](docs/testing/index.md) | xUnit conventions, mocking, assertions, EF Core integration |
| [Package catalogue](docs/index.md) | Complete list of 41 packages with their roles |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for conventions and contribution workflow.

## Changelog

Changes are documented in [CHANGELOG.md](CHANGELOG.md).

## License

Proprietary. All rights reserved. See [LICENSE](LICENSE).
