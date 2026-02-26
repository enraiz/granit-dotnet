# CLAUDE.md - Granit

## Project

- **Type**: Shared NuGet packages for Digital Dynamics .NET applications
- **Repo**: `granit-dotnet` (company-level, not product-specific)
- **Cloud**: OVHcloud (Roubaix, FR) — European sovereignty
- **Compliance**: HDS + RGPD | Criticality: HIGH
- **Publication**: GitLab Package Registry (NuGet)

## Stack & versions

.NET 10 | C# 14 | EF Core 10 | VaultSharp 1.17+ | Serilog 9+ | OpenTelemetry 1.11+

## Packages

| Package | Role |
| ------- | ---- |
| `Granit.Core` | Module system (ABP-inspired), shared domain types |
| `Granit.Timing` | IClock, ICurrentTimezoneProvider, TimeProvider |
| `Granit.Guids` | IGuidGenerator, sequential GUIDs for clustered indexes |
| `Granit.Security` | JWT Keycloak, ICurrentUserService, authorization policies |
| `Granit.Persistence` | EF Core interceptors (HDS audit, RGPD soft delete) |
| `Granit.Vault` | VaultSharp client, ITransitEncryptionService, dynamic credentials |
| `Granit.Observability` | Serilog + OpenTelemetry → OTLP → Loki/Tempo/Mimir |

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

| Content | Language |
| ------- | -------- |
| C# code — identifiers, XML docs (`/// <summary>`), inline comments (`//`) | **English** |
| `docs/**/*.md` | **French** |
| GitLab issues (title, description, comments) | **French** |
| Commits (Conventional Commits messages) | **French** |
| `CLAUDE.md`, skills | **English** |

**Diacritics**: ALWAYS use correct French accents (é, è, ê, à, â, ù, û, ô, î, ï, ç, œ)
in all French content (docs, issues, commits). Never in code.

## Code conventions

**C#**: PascalCase for types and methods, camelCase for parameters and local variables,
`I` prefix for interfaces, `Async` suffix for async methods. Strict Roslyn rules:

- **IDE0008**: ALWAYS use explicit type instead of `var`
  (e.g., `ServiceCollection services = new();` not `var services = new ServiceCollection();`)
- **IDE0022**: Use expression body (`=>`) for single-statement methods
- **ASP0025**: Use `AddAuthorizationBuilder()` instead of `AddAuthorization(Action<AuthorizationOptions>)`

**Projects**: one project = one NuGet package, namespace = project name, zero circular references

**Extension method receiver — `IServiceCollection` vs `IHostApplicationBuilder`**:

- Use `IServiceCollection` for composable library packages (the default). This keeps the
  package usable from any DI setup and avoids coupling to the host.
- Use `IHostApplicationBuilder` only when the package needs `Configuration` **and** registers
  framework-level services (Wolverine, background jobs, Observability, Persistence.Migrations,
  BlobStorage.S3, Webhooks) or calls `ValidateOnStart()`.
- Preferred options-binding pattern for new code:

  ```csharp
  services.AddOptions<TOptions>()
      .BindConfiguration(SectionName)
      .ValidateDataAnnotations()
      .ValidateOnStart();
  ```

**Core**: `Granit.Core` provides the module system and domain types.
Each module is self-contained (interface + implementation in the same package).
All Granit packages reference Core.

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

**Tests**: each package has a test project (`*.Tests`). xUnit + FluentAssertions +
NSubstitute + Bogus. Tests are part of the DoD for every story.

**Markdown**: all `.md` files must comply with markdownlint (config in `.markdownlint.json`).
Verify with `npx markdownlint-cli2 "file.md"` before committing.

## Personas (user stories)

Persona registry: `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`.
15 canonical personas defined.

**STRICT RULES:**

- **ALWAYS** use a canonical persona in user stories (`As a [persona]`)
- **NEVER** introduce a new persona without user validation and registry update
- **NEVER** use hybrid roles (`SRE / DevOps`) — choose the primary persona
- Context (on-call, audit, incident) belongs in the story body, not in the persona

**Available personas:** SRE, Ingénieur DevOps, Développeur, Architecte, DBA, RSSI,
DPO, CTO, Direction, Directeur juridique, Auditeur interne, Auditeur externe,
Utilisateur, Professionnel de santé, Product Owner

## GitLab issues

Before any GitLab operation, **invoke skill `/gitlab`** to load commands and conventions.

- **Types**: Epic (`[EPIC]`), Feature (`[FEATURE]`), Story (`[STORY]`) — no emoji in titles
- **Hierarchy**: GitLab Free — `relates_to` links via API + references in parent description
- **Templates**: `.gitlab/issue_templates/` (Story, Feature, Epic, Bug, Spike, Tech_Debt)

## Definition of Done — mandatory before any push

**NEVER push or create an MR without all of the following being complete:**

1. **Unit tests**: every modified package has its `*.Tests` project updated or extended
   to cover the new/changed behaviour. `dotnet test` passes with zero failures.
2. **Documentation**: any public API change, new feature, or behaviour change is reflected
   in `docs/**/*.md` (French). Create the file if it does not exist; update it otherwise.
3. **Format**: `dotnet format --verify-no-changes` exits with code 0.
4. **Markdownlint**: every modified `.md` file passes `npx markdownlint-cli2 "<file>"`.

These four checks are **blocking**. If the user asks to push without them, remind them
and refuse until the DoD is satisfied or the user explicitly overrides each item.

## Git workflow

- **Branching**: GitFlow (main + develop + `feature/*` + `release/*` + `hotfix/*`)
- **Direct push to `main` FORBIDDEN**
- **Releases**: Semantic tags on main (vMAJOR.MINOR.PATCH), `release/*` branches for stabilization
- **Commits**: Conventional Commits (feat:, fix:, docs:, chore:)
- **MR**: 1 approval minimum for main

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

**ALWAYS:**

- No hardcoded secrets (not even in comments or examples)
- `sensitive = true` on all secret variables
- Logs must not expose secrets or PII

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
