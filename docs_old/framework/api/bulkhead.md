# Bulkhead Isolation

`Granit.Bulkhead` provides per-tenant concurrency isolation for APIs and
Wolverine handlers. Uses `System.Threading.RateLimiting.ConcurrencyLimiter`
(built-in .NET) to prevent a single tenant from monopolizing server resources
(CPU, memory, threads).

> **ISO 27001 compliance**: each rejected request (503) is traced in structured
> logs with the tenant, policy, and concurrency limits. No personal data is
> stored in limiter keys.

## At a glance

- Per-tenant concurrency isolation with in-memory `ConcurrencyLimiter`
- ASP.NET Core endpoint filter (503 Service Unavailable) and Wolverine middleware
- Automatic bypass for machine actors and configurable roles
- Optional queue with timeout per policy
- Plan-based quotas via `Granit.Features` integration

## Installation

```bash
dotnet add package Granit.Bulkhead
```

## Quick setup

```csharp
[DependsOn(typeof(GranitBulkheadModule))]
public sealed class AppModule : GranitModule { }
```

The module automatically calls `AddGranitBulkhead()` reading the `Bulkhead`
section from `appsettings.json`.

### Registration without module

```csharp
builder.Services.AddGranitBulkhead();
```

Or with an explicit configuration section:

```csharp
builder.Services.AddGranitBulkhead(
    builder.Configuration.GetSection("Bulkhead"));
```

## appsettings.json

```json
{
  "Bulkhead": {
    "Enabled": true,
    "BypassRoles": ["SystemAdmin"],
    "UseFeatureBasedQuotas": false,
    "Policies": {
      "api": {
        "PermitLimit": 20,
        "QueueLimit": 10,
        "QueueTimeout": "00:00:30"
      },
      "import": {
        "PermitLimit": 2,
        "QueueLimit": 5,
        "QueueTimeout": "00:01:00"
      },
      "report-generation": {
        "PermitLimit": 3,
        "QueueLimit": 0
      }
    }
  }
}
```

## Usage

### Endpoint filter (Minimal APIs)

```csharp
app.MapGet("/api/v1/reports", GenerateReportAsync)
   .RequireGranitBulkhead("report-generation");
```

When the bulkhead is full, the filter returns:

- **HTTP 503 Service Unavailable** (RFC 7807 Problem Details)
- The client can retry immediately (unlike 429, no `Retry-After`)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.4",
  "title": "Service Unavailable",
  "status": 503,
  "detail": "Bulkhead full for policy 'report-generation'. PermitLimit: 3, QueueLimit: 0."
}
```

### Wolverine middleware

Decorate the message type with `[Bulkhead]`:

```csharp
[Bulkhead("import")]
public sealed record ImportDataCommand(Guid TenantId, Stream Data);
```

Register the middleware in Wolverine setup:

```csharp
opts.Policies.AddMiddleware<BulkheadMiddleware>(
    chain => chain.MessageType
        .GetCustomAttributes(typeof(BulkheadAttribute), true).Length > 0);
```

When the bulkhead is full, `BulkheadRejectedException` is thrown and can be
handled by Wolverine's retry policy (`RetryWithCooldown`).

> [!NOTE]
> **Wolverine lifecycle:** `BeforeAsync` returns a `BulkheadLease`. Wolverine
> automatically injects this return value into `After(BulkheadLease lease)` as
> a parameter. This avoids `AsyncLocal` and guarantees proper disposal even if
> the handler throws.

## How it works

### In-memory per pod

Each application instance maintains its own `ConcurrencyLimiter` instances.
With N pods and `PermitLimit=P`, a tenant can use up to N×P concurrent
operations cluster-wide.

This is by design: the bulkhead protects **local** CPU/memory per pod. For
**distributed** rate limiting (quotas shared across pods), use
`Granit.RateLimiting` (Redis).

```text
Pod 1: [●●●○○] 3/5 slots used (tenant A)
Pod 2: [●○○○○] 1/5 slots used (tenant A)
Pod 3: [○○○○○] 0/5 slots used (tenant A)
→ Tenant A uses 4 concurrent operations cluster-wide (max possible: 15)
```

> [!TIP]
> **Complementary patterns:** use `Granit.RateLimiting` for distributed
> request quotas (e.g. 100 req/min per tenant) and `Granit.Bulkhead` for
> local resource protection (e.g. max 3 concurrent report generations per
> tenant per pod).

### Queue support

When `QueueLimit > 0`, requests that arrive while all concurrency slots are
occupied wait in a FIFO queue instead of being immediately rejected:

```text
PermitLimit: 2, QueueLimit: 3

Active:  [●●]         ← 2 running
Queue:   [③②①]        ← 3 waiting (FIFO)
New req: → REJECTED (queue full, 503)
```

`QueueTimeout` controls how long a request waits in queue before being
rejected. The timeout is implemented via `CancellationTokenSource.CreateLinkedTokenSource`,
combining the request's cancellation token with a timeout-based token.

## Tenant partitioning

Each limiter is keyed by `{policyName}:{tenantId}`:

```text
api:a1b2c3d4-...     → ConcurrencyLimiter (permit=20, queue=10)
api:e5f6g7h8-...     → ConcurrencyLimiter (permit=20, queue=10)
import:a1b2c3d4-...  → ConcurrencyLimiter (permit=2, queue=5)
```

Without multi-tenancy (`ICurrentTenant.IsAvailable = false`), the segment
`global` is used: `api:global`.

## Bypass rules

### Machine actors (automatic)

Machine actors (`ActorKind.System`, `ActorKind.ExternalSystem`) always bypass
the bulkhead. This ensures that CRON jobs, migrations, and system maintenance
are never blocked by tenant quotas.

Detection uses `ICurrentUserService.IsMachine`.

### Bypass roles (configurable)

Users with any of the configured `BypassRoles` skip bulkhead checks:

```json
{
  "Bulkhead": {
    "BypassRoles": ["SystemAdmin", "ServiceAccount"]
  }
}
```

Bypass is logged at `Debug` level with the role and user identifier.

### Bypass chain

```text
Is bulkhead enabled? ─── No ──→ NoOp (pass through)
         │ Yes
Does policy exist? ───── No ──→ NoOp (pass through)
         │ Yes
Is machine actor? ────── Yes ─→ NoOp (bypass logged)
         │ No
Has bypass role? ─────── Yes ─→ NoOp (bypass logged)
         │ No
         ↓
    Acquire permit → Success: BulkheadLease
                   → Full: BulkheadRejectedException (503)
```

## Plan-based quotas

Enable `UseFeatureBasedQuotas` to resolve `PermitLimit` from `Granit.Features`:

```json
{
  "Bulkhead": {
    "UseFeatureBasedQuotas": true
  }
}
```

The provider looks for a Numeric feature named `Bulkhead.{policyName}`
(convention) or the name defined in `FeatureName` of the policy.

```csharp
// In a FeatureDefinitionProvider
context.Add(
    new FeatureDefinition("Bulkhead.api", FeatureValueType.Numeric(20, 1, 100))
);
```

If the feature does not exist or `IFeatureChecker` is not registered, the
provider falls back to the static `PermitLimit` from configuration.

> [!NOTE]
> **Under the hood:** `IFeatureChecker` is resolved via `IServiceProvider.GetService()`
> (soft dependency). The module works without `Granit.Features` installed. A
> one-time warning is logged if `UseFeatureBasedQuotas` is enabled but
> `IFeatureChecker` is not registered.

## Idle limiter cleanup

A `BackgroundService` runs every `CleanupInterval` (default: 5 minutes) and
evicts limiters that have not been used for longer than `IdleTimeout`
(default: 30 minutes). This prevents memory leaks from tenants that connect
once and never return.

```json
{
  "Bulkhead": {
    "IdleTimeout": "00:30:00",
    "CleanupInterval": "00:05:00"
  }
}
```

## 503 vs 429

The bulkhead returns **503 Service Unavailable**, not 429 Too Many Requests:

| Aspect | 503 (Bulkhead) | 429 (Rate Limiting) |
| --- | --- | --- |
| Protects | Server resources (CPU, memory) | Client quotas (requests/time) |
| Retry strategy | Immediate retry OK | Wait for `Retry-After` |
| Scope | Per pod (local) | Per cluster (distributed) |
| Pattern | Bulkhead | Rate Limiting |

## Observability

### Metrics (`System.Diagnostics.Metrics`)

| Counter | Description | Tags |
| --- | --- | --- |
| `granit.bulkhead.leases.active` | Currently active bulkhead leases (up/down) | `policy`, `tenant_id` |
| `granit.bulkhead.requests.rejected` | Requests rejected by bulkhead isolation | `policy`, `tenant_id` |

Meter: `Granit.Bulkhead`

### Structured logs (source-generated)

| Level | Message | Context |
| --- | --- | --- |
| Warning | Bulkhead rejected | `PolicyName`, `TenantId`, `PermitLimit`, `QueueLimit` |
| Warning | Feature checker missing | `UseFeatureBasedQuotas` enabled but `IFeatureChecker` not registered |
| Debug | Bypass applied | `PolicyName`, `Reason`, `UserId` |
| Debug | Lease acquired | `PolicyName`, `TenantId` |
| Debug | Lease released | `PolicyName`, `TenantId` |
| Information | Idle limiters evicted | `EvictedCount` |

## Configuration options

### `GranitBulkheadOptions` (section `Bulkhead`)

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Enabled` | `bool` | `true` | Enable/disable bulkhead isolation globally |
| `BypassRoles` | `string[]` | `[]` | Roles exempt from bulkhead checks |
| `UseFeatureBasedQuotas` | `bool` | `false` | Resolve limits from `Granit.Features` |
| `Policies` | `Dictionary<string, BulkheadPolicyOptions>` | `{}` | Named policies (case-insensitive) |
| `IdleTimeout` | `TimeSpan` | `30 min` | Idle duration before limiter eviction |
| `CleanupInterval` | `TimeSpan` | `5 min` | Interval between cleanup sweeps |

### `BulkheadPolicyOptions`

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `PermitLimit` | `int` | `10` | Max concurrent operations per tenant (1–10,000) |
| `QueueLimit` | `int` | `0` | Max queued requests when full (0 = no queue) |
| `QueueTimeout` | `TimeSpan` | `30 s` | Max queue wait before rejection |
| `FeatureName` | `string?` | `null` | Feature name override (default: `Bulkhead.{policy}`) |

## Architecture

```text
src/
  Granit.Bulkhead/
  ├── Abstractions/
  │   ├── BulkheadLease.cs               (IDisposable, thread-safe)
  │   └── IBulkheadQuotaProvider.cs      (dynamic permit limit resolution)
  ├── AspNetCore/
  │   └── BulkheadEndpointExtensions.cs  (RequireGranitBulkhead)
  ├── Attributes/
  │   └── BulkheadAttribute.cs           ([Bulkhead("policy")])
  ├── Exceptions/
  │   ├── BulkheadRejectedException.cs   (BusinessException → 503)
  │   └── BulkheadExceptionStatusCodeMapper.cs
  ├── Extensions/
  │   └── BulkheadServiceCollectionExtensions.cs  (AddGranitBulkhead)
  ├── Internal/
  │   ├── ConcurrencyLimiterRegistry.cs     (singleton, ConcurrentDictionary)
  │   ├── TenantPartitionedBulkhead.cs      (scoped orchestrator)
  │   ├── BulkheadMetrics.cs                (OTel counters)
  │   ├── BulkheadLog.cs                    (LoggerMessage source-generated)
  │   ├── FeatureBasedBulkheadQuotaProvider.cs  (Granit.Features)
  │   ├── OptionsBulkheadQuotaProvider.cs   (static config)
  │   ├── GranitBulkheadOptionsValidator.cs (IValidateOptions)
  │   └── BulkheadCleanupService.cs         (BackgroundService, eviction)
  ├── Options/
  │   ├── GranitBulkheadOptions.cs
  │   └── BulkheadPolicyOptions.cs
  ├── Wolverine/
  │   └── BulkheadMiddleware.cs            (BeforeAsync/After, return value)
  └── GranitBulkheadModule.cs

tests/
  Granit.Bulkhead.Tests/                   (41 tests)
```

## Registered services

### `GranitBulkheadModule`

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `ConcurrencyLimiterRegistry` | — | Singleton |
| `TenantPartitionedBulkhead` | — | Scoped |
| `IBulkheadQuotaProvider` | `OptionsBulkheadQuotaProvider` or `FeatureBasedBulkheadQuotaProvider` | Scoped |
| `BulkheadMetrics` | — | Singleton |
| `IExceptionStatusCodeMapper` | `BulkheadExceptionStatusCodeMapper` | Singleton |
| `IValidateOptions<GranitBulkheadOptions>` | `GranitBulkheadOptionsValidator` | Singleton |
| `IHostedService` | `BulkheadCleanupService` | Singleton |

`IBulkheadQuotaProvider` and `TenantPartitionedBulkhead` are `Scoped` to
align with `ICurrentTenant` and `ICurrentUserService`.

## Security

- Limiter keys contain **no personal data** — only the policy name and
  tenant ID (GUID) or `global`.
- Idle limiters are automatically evicted — no memory accumulation.
- Bypass is **logged** (audit trail) with the user identifier and reason.
- `BulkheadRejectedException` does not expose sensitive information in the
  error message.

## Granit dependencies

| Direction | Modules |
| --- | --- |
| **Depends on** | `Granit.Core`, `Granit.ExceptionHandling`, `Granit.Features`, `Granit.Security` |
| **Used by** | Leaf module (consumed by applications) |

> See the [full dependency graph](../dependencies.md).
