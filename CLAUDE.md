# CLAUDE.md - Granit

## Project

- **Type**: Shared NuGet packages for Digital Dynamics .NET applications
- **Repo**: `granit-dotnet` (company-level, not product-specific)
- **Cloud**: OVHcloud (Roubaix, FR) — European sovereignty
- **Compliance**: HDS + RGPD | Criticality: HIGH
- **Publication**: GitLab Package Registry (NuGet)

## GitLab repositories

| ID | Repo | Path |
| -- | ---- | ---- |
| 5 | governance-compliance | `digital-dynamics/governance-compliance` |
| 6 | **granit-dotnet** | `digital-dynamics/granit-dotnet` |
| 9 | granit-front | `digital-dynamics/granit-front` |
| 10 | guava-admin | `digital-dynamics/guava-platform/applications/guava-admin` |
| 4 | guava-app-template | `digital-dynamics/guava-platform/applications/guava-app-template` |
| 7 | guava-backend | `digital-dynamics/guava-platform/applications/guava-backend` |
| 1 | guava-front | `digital-dynamics/guava-platform/applications/guava-front` |
| 3 | gitops | `digital-dynamics/guava-platform/infrastructure/gitops` |
| 2 | iac | `digital-dynamics/guava-platform/infrastructure/iac` |
| 8 | project-governance | `digital-dynamics/guava-platform/project-governance` |

## Stack & versions

.NET 10 | C# 14 | EF Core 10 | VaultSharp 1.17+ | Serilog 9+ | OpenTelemetry 1.11+

## Packages (86 packages)

### Core & utilities

| Package | Role |
| ------- | ---- |
| `Granit.Core` | Module system (ABP-inspired), shared domain types |
| `Granit.Timing` | IClock, ICurrentTimezoneProvider, TimeProvider |
| `Granit.Guids` | IGuidGenerator, sequential GUIDs for clustered indexes |
| `Granit.Validation` | FluentValidation integration |
| `Granit.Analyzers` / `.CodeFixes` | Custom Roslyn analyzers and code fixes |

### Security & authentication

| Package | Role |
| ------- | ---- |
| `Granit.Security` | ICurrentUserService, shared security abstractions |
| `Granit.Authentication.JwtBearer` | JWT Bearer authentication middleware |
| `Granit.Authentication.Keycloak` | Keycloak claims transformation |
| `Granit.Authorization` / `.EntityFrameworkCore` | Policy-based authorization, EF Core store |
| `Granit.Vault` | VaultSharp, ITransitEncryptionService, dynamic credentials |
| `Granit.Encryption` | Data encryption abstractions |
| `Granit.Privacy` | RGPD privacy helpers |

### Identity

| Package | Role |
| ------- | ---- |
| `Granit.Identity` | Identity provider abstractions (IIdentityProvider, IUserLookupService, models) |
| `Granit.Identity.Keycloak` | Keycloak Admin API implementation of IIdentityProvider |
| `Granit.Identity.EntityFrameworkCore` | EF Core user cache (cache-aside, login-time sync, RGPD) |
| `Granit.Identity.Endpoints` | Minimal API endpoints for user cache (CRUD, sync, RGPD, webhook, stats) |

### Data & persistence

| Package | Role |
| ------- | ---- |
| `Granit.Persistence` / `.Migrations` / `.Migrations.Wolverine` | EF Core interceptors (HDS audit, soft delete), migrations |
| `Granit.Caching` / `.Hybrid` / `.StackExchangeRedis` | Distributed caching (IDistributedCache, HybridCache, Redis) |
| `Granit.MultiTenancy` | Tenant isolation, ICurrentTenant |
| `Granit.Settings` / `.EntityFrameworkCore` | Application settings management |
| `Granit.Features` / `.EntityFrameworkCore` | Feature management (Toggle/Numeric/Selection) |
| `Granit.ReferenceData` / `.Endpoints` / `.EntityFrameworkCore` | Reference data (i18n labels, CRUD) |

### API & web

| Package | Role |
| ------- | ---- |
| `Granit.ApiVersioning` | Asp.Versioning integration |
| `Granit.ApiDocumentation` | Scalar OpenAPI documentation |
| `Granit.ExceptionHandling` | RFC 7807 Problem Details |
| `Granit.Idempotency` | Idempotency-Key middleware |
| `Granit.Cors` | CORS policy configuration |
| `Granit.Cookies` / `.Klaro` | Cookie consent management |

### Messaging & events

| Package | Role |
| ------- | ---- |
| `Granit.Wolverine` / `.Postgresql` | Wolverine messaging, transactional outbox |
| `Granit.Webhooks` / `.EntityFrameworkCore` | Webhook subscriptions and delivery |
| `Granit.Notifications` / `.Endpoints` / `.EntityFrameworkCore` | Notification engine (fan-out, delivery tracking) |
| `Granit.Notifications.Email` / `.Email.Smtp` / `.Brevo` | Email channels (SMTP, Brevo) |
| `Granit.Notifications.Sms` / `.WhatsApp` / `.Push` | SMS, WhatsApp, Web Push channels |
| `Granit.Notifications.SignalR` | Real-time SignalR channel |

### Documents & templates

| Package | Role |
| ------- | ---- |
| `Granit.Templating` / `.Scriban` / `.EntityFrameworkCore` / `.Workflow` | Template engine (Scriban), EF store, workflow integration |
| `Granit.DocumentGeneration` / `.Pdf` / `.Excel` | Document rendering (HTML→PDF, Excel) |

### Data exchange (import/export)

| Package | Role |
| ------- | ---- |
| `Granit.DataExchange` / `.Csv` / `.Excel` / `.EntityFrameworkCore` / `.Endpoints` | Import: Extract→Map→Validate→Execute (Sep, Sylvan). Export: tabular Excel/CSV with presets and background jobs |

### Workflow

| Package | Role |
| ------- | ---- |
| `Granit.Workflow` / `.Endpoints` / `.EntityFrameworkCore` / `.Notifications` | FSM engine, publication lifecycle |

### Diagnostics & observability

| Package | Role |
| ------- | ---- |
| `Granit.Observability` | Serilog + OpenTelemetry → OTLP → Loki/Tempo/Mimir |
| `Granit.Diagnostics` | Health checks, readiness probes |
| `Granit.Timeline` / `.Endpoints` / `.EntityFrameworkCore` / `.Notifications` | Audit timeline (HDS) |

### Storage & imaging

| Package | Role |
| ------- | ---- |
| `Granit.BlobStorage` / `.S3` / `.EntityFrameworkCore` | Blob storage (S3-compatible), metadata EF store |
| `Granit.Imaging` / `.MagickNet` | Image processing (WebP/AVIF, EXIF stripping) |

### Scheduling & jobs

| Package | Role |
| ------- | ---- |
| `Granit.BackgroundJobs` / `.Endpoints` / `.EntityFrameworkCore` | Background job scheduling (Wolverine + Cronos) |

### Localization

| Package | Role |
| ------- | ---- |
| `Granit.Localization` / `.Endpoints` / `.EntityFrameworkCore` / `.SourceGenerator` | i18n (9 cultures: 7 base + regional variants), override store, source-generated keys |

## Commands

```bash
dotnet build
dotnet test
dotnet pack -c Release -o ./nupkgs   # local NuGet pack
dotnet format --verify-no-changes
```

## Regulatory constraints — CRITICAL

1. **Sovereignty**: Infrastructure MUST stay in Europe (OVHcloud FR)
2. **US Cloud Act**: NEVER use AWS/Azure/GCP for health data
3. **HDS**: 3-year audit trail, encryption at rest and in transit
4. **RGPD**: Minimization, right to erasure, pseudonymization
5. **Secrets**: No plaintext secrets, mandatory rotation

## Language

See [`docs/guide/conventions/langues.md`](docs/guide/conventions/langues.md) for full language and localization rules.

- **Code** (identifiers, XML docs, comments): **English**
- **Docs, issues, commits**: **French** (with correct diacritics: é, è, ê, à, â, ù, û, ô, î, ï, ç, œ)
- **`CLAUDE.md`, skills**: **English**
- **Localization**: **9 cultures** — 7 base languages (en, fr, nl, de, es, it, pt) +
  2 regional variants (fr-CA, en-GB). Every `src/*/Localization/**/*.json` must
  exist for all 9 files. Regional files only contain keys that differ from the base.
  No `en-US.json` needed — `en.json` is already US English (`en-US` → `en` fallback).
- `ReferenceDataEntity` translations: `LabelEn`, `LabelFr`, `LabelNl`, `LabelDe`,
  `LabelEs`, `LabelIt`, `LabelPt` (7 properties — regional variants use
  `TwoLetterISOLanguageName` fallback: fr-CA → LabelFr, en-GB → LabelEn)
- **Governance**: [`docs/framework/utilities/localization/gouvernance.md`](docs/framework/utilities/localization/gouvernance.md) (framework)
  and [`docs/guide/gouvernance-traductions.md`](docs/guide/gouvernance-traductions.md) (application)

## Code conventions

Full coding standards: [`docs/guide/conventions/`](docs/guide/conventions/index.md) (backend: style-et-nommage, architecture, implementation)

Key rules for quick reference:

- **IDE0008**: Use `var` when type is apparent; explicit type otherwise (Microsoft rule)
- **IDE0022**: Expression body (`=>`) for single-statement methods
- **ASP0025**: Use `AddAuthorizationBuilder()` instead of `AddAuthorization(Action<>)`
- **Regex**: ALWAYS `[GeneratedRegex]`, never `new Regex(..., Compiled)`. Timeout on user input.
- **Logging**: ALWAYS `[LoggerMessage]` source-generated, never string interpolation in log calls
- **Time**: NEVER `DateTime.Now`/`UtcNow` — inject `TimeProvider` or `IClock`
- **Async**: `ConfigureAwait(false)` in library code, `CancellationToken` as last param
- **Guards**: Prefer `ArgumentNullException.ThrowIfNull()` over manual null checks
- **Projects**: one project = one NuGet package, namespace = project name, zero circular refs
- **Tests**: each package has `*.Tests` (xUnit + Shouldly + NSubstitute + Bogus). Part of DoD.
- **Markdown**: all `.md` must pass `npx markdownlint-cli2 "file.md"` before committing
- **Endpoint DTOs**: Module-specific DTOs must be prefixed with module context (`WorkflowTransitionRequest`, not `TransitionRequest`). OpenAPI flattens namespaces — generic names cause schema conflicts. Shared cross-cutting types (`PagedResult<T>`, `ProblemDetails`) are exempt.
- **DTO suffixes**: `Request` for input bodies, `Response` for top-level returns. NEVER use `Dto` suffix. EF Core entities must NOT be returned directly — create a `*Response` record.
- **Error responses**: Always `TypedResults.Problem(detail, statusCode)` (RFC 7807), never `TypedResults.BadRequest<string>()`. Return type: `ProblemHttpResult`.

**Isolated DbContext pattern — MANDATORY for all `*.EntityFrameworkCore` packages**:

Every Granit `*.EntityFrameworkCore` package that owns an isolated `DbContext` MUST follow this
checklist (no exceptions):

1. **`<ProjectReference>` to `Granit.Persistence`** in the `.csproj`.
2. **Constructor injection** of `ICurrentTenant?` and `IDataFilter?` (both optional, default `null`).
3. **Call `modelBuilder.ApplyGranitConventions(currentTenant, dataFilter)`** at the end of
   `OnModelCreating` — this applies query filters for `ISoftDeletable`, `IMultiTenant`, `IActive`,
   `IProcessingRestrictable`, and `IPublishable`.
4. **Interceptor wiring** in the extension method: use the `(sp, options)` overload of
   `AddDbContextFactory` with `ServiceLifetime.Scoped` and resolve `AuditedEntityInterceptor` /
   `SoftDeleteInterceptor` from the service provider.
5. **`[DependsOn(typeof(GranitPersistenceModule))]`** on the module class.
6. **No manual `HasQueryFilter`** in entity configurations — `ApplyGranitConventions` handles all
   standard filters centrally. Manual filters cause duplicates or conflicts.
7. **`IMultiTenant`** entities use `Guid? TenantId` (never `string`). The interface lives in
   `Granit.Core.Domain`. `IDataFilter` lives in `Granit.Core.DataFiltering`.

Reference: [`docs/framework/data/persistence.md`](docs/framework/data/persistence.md)

**Multi-tenancy — soft dependency rule**: `ICurrentTenant` lives in `Granit.Core.MultiTenancy`
and is available in every module without referencing `Granit.MultiTenancy`.

- New Granit modules that read `ICurrentTenant`: use `using Granit.Core.MultiTenancy;`, do NOT
  add `[DependsOn(typeof(GranitMultiTenancyModule))]` or a `<ProjectReference>` to
  `Granit.MultiTenancy`. A `NullTenantContext` (`IsAvailable = false`) is registered by default.
- Always check `IsAvailable` before using `Id` — the null object is the normal state when
  multi-tenancy is not installed.
- Hard dependency on `Granit.MultiTenancy` is allowed **only** when the module must enforce
  strict tenant isolation (example: BlobStorage — throws if no tenant context, RGPD/HDS).
- Application modules (`GuavaHostModule`, etc.) declare `[DependsOn(GranitMultiTenancyModule)]`
  as usual when multi-tenancy is required in the application.

## Personas (user stories)

Two persona registries:

- **Infrastructure & governance (15 personas)**: `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`
- **Application-level (5 personas)**: [`docs/guide/personas-applicatifs.md`](docs/guide/personas-applicatifs.md)

**STRICT RULES:**

- **ALWAYS** use a canonical persona in user stories (`As a [persona]`)
- **NEVER** introduce a new persona without user validation and registry update
- **NEVER** use hybrid roles (`SRE / DevOps`) — choose the primary persona
- Context (on-call, audit, incident) belongs in the story body, not in the persona

**Infra/governance personas:** SRE, Ingénieur DevOps, Développeur, Architecte, DBA,
RSSI, DPO, CTO, Direction, Directeur juridique, Auditeur interne, Auditeur externe,
Utilisateur, Professionnel de santé, Product Owner

**Application personas:** Visiteur, Utilisateur authentifié, Administrateur d'application,
Approbateur, Gestionnaire de contenu

## GitLab issues

Before any GitLab operation, **invoke skill `/gitlab`** to load commands and conventions.

- **Types**: Epic (`[EPIC]`), Feature (`[FEATURE]`), Story (`[STORY]`) — no emoji in titles
- **Hierarchy**: GitLab Free — `relates_to` links via API + references in parent description
- **Templates**: `.gitlab/issue_templates/` (Story, Feature, Epic, Bug, Spike, Tech_Debt)

## Definition of Done — mandatory before any push

See [`docs/guide/conventions/dod.md`](docs/guide/conventions/dod.md) for full details.

**NEVER push or create an MR** without: tests passing, docs updated, format clean, markdownlint clean.
These four checks are **blocking**. If the user asks to push without them, remind them
and refuse until the DoD is satisfied or the user explicitly overrides each item.

## Git workflow

See [`docs/guide/conventions/workflow.md`](docs/guide/conventions/workflow.md) for branching and commit conventions.

- **Direct push to `main` FORBIDDEN**
- **MR**: 1 approval minimum for main
- **Releases**: Semantic tags on main (vMAJOR.MINOR.PATCH)

**MR target — STRICT RULE:**

| Branch type | Default target | Exception |
| ----------- | -------------- | --------- |
| `feature/*` | `develop` | Only if user explicitly says "target main" |
| `hotfix/*` | `main` + `develop` | Both, always |
| `release/*` | `main` + `develop` | Both, always |
| `fix/*` | `develop` | Only if user explicitly says "target main" |

NEVER target `main` for a `feature/*` or `fix/*` branch unless the user explicitly
requests it. When in doubt, ask before creating the MR.

## Security — strict rules

See [`docs/guide/conventions/securite.md`](docs/guide/conventions/securite.md) for code-level security rules.

**NEVER:**

- Propose US cloud solutions (AWS/Azure/GCP) for health data
- Store secrets in plain text
- Disable audit logging
- Propose quick fixes that create technical debt

## Refactoring — mandatory rules

Code that looks "weird" almost always exists for a reason: production fix, regulatory
edge case, HDS/RGPD constraint, third-party limitation workaround. Never remove or
rewrite code without understanding the original intent.

**Before any refactoring:**

1. Read the entire file for context, not just the targeted function
2. Check git history (`git log -p -- <file>`) to understand how the code evolved
3. If intent remains unclear, search for the linked GitLab issue (number in commit,
   file label, or keyword search) before modifying
4. When in doubt, ask rather than assuming the code is useless

**NEVER:**

- Delete "dead" code without verifying it is not referenced dynamically
  (reflection, DI, runtime configuration)
- Simplify a complex condition without having tested the edge cases it covers
- Replace a custom implementation with a standard library without verifying why
  the library was not used initially

## Expected behavior

- Understand HDS, RGPD, ISO 27001 and ISO 9001 context before responding
- Challenge security bad practices
- Propose alternatives when a request compromises security
- Explain the "why" behind best practices
- Provide production-ready code (no TODOs, no obvious comments)
