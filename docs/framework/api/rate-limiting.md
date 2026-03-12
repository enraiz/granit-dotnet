# Rate Limiting

`Granit.RateLimiting` fournit un rate limiting par tenant pour les API et les
handlers Wolverine. Trois algorithmes (sliding window, fixed window, token bucket)
implémentés via des scripts Lua Redis atomiques, avec dégradation gracieuse
configurable et quotas dynamiques par plan via `Granit.Features`.

> **Conformité ISO 27001** : chaque requête rejetée (429) est tracée dans les logs
> structurés avec le tenant, la policy et le temps de retry. Les compteurs Redis
> utilisent un préfixe configurable et un TTL automatique — aucune donnée
> personnelle n'est stockée dans les clés.

## En bref

- Rate limiting per-tenant avec partitionnement Redis Cluster (`{tenantId}`)
- 3 algorithmes : sliding window (sorted set), fixed window (INCR), token bucket (hash)
- Intégration ASP.NET Core (endpoint filter + `Retry-After`) et Wolverine (middleware)

## Installation

```bash
dotnet add package Granit.RateLimiting
```

## Configuration rapide

```csharp
[DependsOn(typeof(GranitRateLimitingModule))]
public sealed class AppModule : GranitModule { }
```

Le module appelle automatiquement `AddGranitRateLimiting()` en lisant la section
`RateLimiting` de `appsettings.json`.

### Enregistrement sans module

```csharp
builder.Services.AddGranitRateLimiting();
```

Ou avec une section de configuration explicite :

```csharp
builder.Services.AddGranitRateLimiting(
    builder.Configuration.GetSection("RateLimiting"));
```

## appsettings.json

```json
{
  "RateLimiting": {
    "Enabled": true,
    "KeyPrefix": "rl",
    "FallbackOnCounterStoreFailure": "Allow",
    "BypassRoles": ["Admin"],
    "UseFeatureBasedQuotas": false,
    "Policies": {
      "api": {
        "Algorithm": "SlidingWindow",
        "PermitLimit": 100,
        "Window": "00:01:00",
        "SegmentsPerWindow": 6
      },
      "auth": {
        "Algorithm": "FixedWindow",
        "PermitLimit": 5,
        "Window": "00:15:00"
      },
      "export": {
        "Algorithm": "TokenBucket",
        "TokenLimit": 10,
        "TokensPerPeriod": 2,
        "ReplenishmentPeriod": "00:00:30"
      }
    }
  }
}
```

## Utilisation

### Endpoint filter (Minimal APIs)

```csharp
app.MapGet("/api/v1/patients", GetPatientsAsync)
   .RequireGranitRateLimiting("api");
```

Quand le quota est dépassé, le filtre retourne automatiquement :

- **HTTP 429 Too Many Requests** (RFC 7807 Problem Details)
- Header `Retry-After` (secondes)
- Extensions : `policy`, `limit`, `remaining`, `retryAfter`

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded for policy 'api'. Retry after 42s.",
  "policy": "api",
  "limit": 100,
  "remaining": 0,
  "retryAfter": 42
}
```

### Wolverine middleware

Décorer le type de message avec `[RateLimited]` :

```csharp
[RateLimited("export")]
public sealed record GeneratePatientExportCommand(Guid PatientId);
```

Enregistrer le middleware dans la configuration Wolverine :

```csharp
opts.Policies.AddMiddleware<RateLimitMiddleware>(
    chain => chain.MessageType
        .GetCustomAttributes(typeof(RateLimitedAttribute), true).Length > 0);
```

Quand le quota est dépassé, `RateLimitExceededException` est levée et peut être
gérée par la politique de retry Wolverine (`RetryWithCooldown`).

## Algorithmes

### Sliding Window

Utilise un **sorted set** Redis. Chaque requête ajoute un membre avec le timestamp
serveur (`redis.call('TIME')`). Les membres expirés sont supprimés par
`ZREMRANGEBYSCORE`. Le plus précis des trois algorithmes.

```text
|◄───── window (60s) ─────►|
│  ●  ●●  ●  ●  ●●●  ●   │→ temps
│◄── vieux supprimés       │
```

| Paramètre | Description | Défaut |
| --- | --- | --- |
| `PermitLimit` | Nombre maximal de requêtes dans la fenêtre | 1000 |
| `Window` | Durée de la fenêtre glissante | 1 min |

### Fixed Window

Utilise un compteur atomique (`INCR`) avec un `PEXPIRE` automatique. Léger en
mémoire mais sujet aux pics en bordure de fenêtre (burst).

| Paramètre | Description | Défaut |
| --- | --- | --- |
| `PermitLimit` | Nombre maximal de requêtes dans la fenêtre | 1000 |
| `Window` | Durée de la fenêtre fixe | 1 min |

### Token Bucket

Utilise un **hash** Redis avec deux champs (`tokens`, `last_refill`). Les tokens
sont rechargés périodiquement jusqu'à un maximum (`TokenLimit`). Idéal pour
autoriser des rafales contrôlées.

```text
Bucket: [●●●●●○○○○○]  (5/10 tokens)
         ↓ requête    → [●●●●○○○○○○]  (4/10)
         ↓ refill     → [●●●●●●○○○○]  (6/10, +2 tokens)
```

| Paramètre | Description | Défaut |
| --- | --- | --- |
| `TokenLimit` | Capacité maximale du bucket | 50 |
| `TokensPerPeriod` | Tokens ajoutés par période | 10 |
| `ReplenishmentPeriod` | Intervalle entre les rechargements | 10 s |

> [!TIP]
> **Choix de l'algorithme :** Sliding Window pour les API publiques (précision),
> Fixed Window pour les endpoints à faible volume (simplicité), Token Bucket
> pour les jobs d'export (rafales contrôlées).

## Partitionnement par tenant

Chaque clé Redis est partitionnée par tenant :

```text
{prefix}:{tenantId}:{policyName}
```

Exemple : `rl:{a1b2c3d4-...}:api`

Les **hash tags** Redis (`{tenantId}`) garantissent que toutes les clés d'un même
tenant sont routées vers le même slot en Redis Cluster.

Sans multi-tenancy (`ICurrentTenant.IsAvailable = false`), le segment `global`
est utilisé : `rl:{global}:api`.

## Quotas dynamiques par plan

Activer `UseFeatureBasedQuotas` pour résoudre les limites depuis `Granit.Features` :

```json
{
  "RateLimiting": {
    "UseFeatureBasedQuotas": true
  }
}
```

Le provider cherche un feature Numeric nommé `RateLimit.{policyName}` (convention)
ou le nom défini dans `FeatureName` de la policy.

```csharp
// Dans un FeatureDefinitionProvider
context.Add(
    new FeatureDefinition("RateLimit.api", FeatureValueType.Numeric(100, 10, 10000))
);
```

Si le feature n'existe pas ou si `IFeatureChecker` n'est pas enregistré, le
provider retombe sur la valeur statique `PermitLimit` de la configuration.

> [!NOTE]
> **Sous le capot :** `IFeatureChecker` est résolu via `IServiceProvider.GetService()`
> (dépendance souple). Le module fonctionne sans `Granit.Features` installé.

## Dégradation gracieuse

Quand Redis est indisponible, le comportement est configurable :

| `FallbackOnCounterStoreFailure` | Comportement |
| --- | --- |
| `Allow` (défaut) | Requête autorisée + warning dans les logs |
| `Deny` | Requête rejetée avec 429 |

Le mode `Allow` (open degradation) évite qu'une panne Redis provoque une
indisponibilité totale du service. Le mode `Deny` (closed degradation) est
préférable pour les endpoints critiques où le dépassement de quota est
inacceptable.

> [!WARNING]
> **Attention :** en mode `Allow`, une panne Redis prolongée désactive de fait le
> rate limiting. Surveillez l'alerte `granit.ratelimiting.requests.allowed` avec
> un tag `fallback` dans vos dashboards.

## Bypass par rôle

Les utilisateurs ayant l'un des `BypassRoles` configurés ne sont pas soumis au
rate limiting :

```json
{
  "RateLimiting": {
    "BypassRoles": ["Admin", "ServiceAccount"]
  }
}
```

Le bypass est loggué au niveau `Debug` avec le rôle et l'identifiant utilisateur.

## Détection automatique Redis / In-Memory

L'enregistrement DI détecte automatiquement la présence de
`IConnectionMultiplexer` dans le conteneur :

- **Redis disponible** → `RedisRateLimitCounterStore` (scripts Lua atomiques)
- **Pas de Redis** → `InMemoryRateLimitCounterStore` (développement, tests)

> [!WARNING]
> **Attention :** le store in-memory n'est pas partagé entre les instances.
> En production multi-pods, les compteurs seront isolés par instance — un
> utilisateur pourra effectuer N × (nombre de pods) requêtes avant d'être limité.

## Observabilité

### Métriques (`System.Diagnostics.Metrics`)

| Compteur | Description | Tags |
| --- | --- | --- |
| `granit.ratelimiting.requests.allowed` | Requêtes autorisées | `policy`, `tenant_id` |
| `granit.ratelimiting.requests.rejected` | Requêtes rejetées (429) | `policy`, `tenant_id` |

Meter : `Granit.RateLimiting`

### Logs structurés (source-generated)

| Niveau | Message | Contexte |
| --- | --- | --- |
| Warning | Rate limit exceeded | `PolicyName`, `TenantId`, `Remaining`, `RetryAfterSeconds` |
| Warning | Counter store unavailable | `PolicyName`, exception |
| Debug | Rate limiting bypassed | `PolicyName`, `Claim`, `UserId` |
| Trace | Rate limit checked | `PolicyName`, `TenantId`, `Remaining`, `Limit` |

## Options de configuration

### `GranitRateLimitingOptions` (section `RateLimiting`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `Enabled` | `bool` | `true` | Active/désactive le rate limiting globalement |
| `KeyPrefix` | `string` | `"rl"` | Préfixe des clés Redis |
| `FallbackOnCounterStoreFailure` | `CounterStoreFailureBehavior` | `Allow` | Comportement si Redis est indisponible |
| `BypassRoles` | `string[]` | `[]` | Rôles exemptés du rate limiting |
| `UseFeatureBasedQuotas` | `bool` | `false` | Résoudre les limites depuis `Granit.Features` |
| `Policies` | `Dictionary<string, RateLimitPolicyOptions>` | `{}` | Policies nommées (case-insensitive) |

### `RateLimitPolicyOptions`

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `Algorithm` | `RateLimitAlgorithm` | `SlidingWindow` | Algorithme à utiliser |
| `PermitLimit` | `int` | `1000` | Requêtes autorisées par fenêtre |
| `Window` | `TimeSpan` | `1 min` | Durée de la fenêtre (sliding/fixed) |
| `SegmentsPerWindow` | `int` | `6` | Segments par fenêtre glissante |
| `TokenLimit` | `int` | `50` | Capacité maximale du bucket |
| `TokensPerPeriod` | `int` | `10` | Tokens rechargés par période |
| `ReplenishmentPeriod` | `TimeSpan` | `10 s` | Intervalle de rechargement |
| `FeatureName` | `string?` | `null` | Nom du feature override (défaut : `RateLimit.{policy}`) |

## Architecture

```text
src/
  Granit.RateLimiting/
  ├── Abstractions/
  │   ├── IRateLimitCounterStore.cs     (check + increment atomique)
  │   ├── IRateLimitQuotaProvider.cs    (résolution du permit limit)
  │   └── RateLimitResult.cs            (IsAllowed, Remaining, Limit, RetryAfter)
  ├── AspNetCore/
  │   └── RateLimitEndpointExtensions.cs  (RequireGranitRateLimiting)
  ├── Attributes/
  │   └── RateLimitedAttribute.cs       ([RateLimited("policy")])
  ├── Exceptions/
  │   ├── RateLimitExceededException.cs (BusinessException, PolicyName, RetryAfter)
  │   └── RateLimitExceptionStatusCodeMapper.cs  (→ 429)
  ├── Extensions/
  │   ├── RateLimitingServiceCollectionExtensions.cs  (AddGranitRateLimiting)
  │   └── RateLimitingApplicationBuilderExtensions.cs (UseGranitRateLimiting)
  ├── Internal/
  │   ├── InMemoryRateLimitCounterStore.cs   (dev/test, ConcurrentDictionary)
  │   ├── RedisRateLimitCounterStore.cs      (Lua scripts, fallback)
  │   ├── LuaScripts.cs                     (3 scripts Lua atomiques)
  │   ├── OptionsRateLimitQuotaProvider.cs   (lecture statique)
  │   ├── FeatureBasedRateLimitQuotaProvider.cs (Granit.Features)
  │   ├── TenantPartitionedRateLimiter.cs    (logique centrale)
  │   ├── RateLimitingMetrics.cs             (compteurs OTel)
  │   └── RateLimitingLog.cs                 (LoggerMessage source-generated)
  ├── Options/
  │   ├── GranitRateLimitingOptions.cs
  │   ├── RateLimitPolicyOptions.cs
  │   ├── RateLimitAlgorithm.cs              (enum)
  │   └── CounterStoreFailureBehavior.cs     (enum)
  ├── Wolverine/
  │   └── RateLimitMiddleware.cs             (BeforeAsync, [RateLimited])
  └── GranitRateLimitingModule.cs

tests/
  Granit.RateLimiting.Tests/               (38 tests)
```

## Services enregistrés

### `GranitRateLimitingModule`

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IRateLimitCounterStore` | `RedisRateLimitCounterStore` ou `InMemoryRateLimitCounterStore` | Scoped |
| `IRateLimitQuotaProvider` | `OptionsRateLimitQuotaProvider` ou `FeatureBasedRateLimitQuotaProvider` | Scoped |
| `TenantPartitionedRateLimiter` | — | Scoped |
| `RateLimitingMetrics` | — | Singleton |
| `IExceptionStatusCodeMapper` | `RateLimitExceptionStatusCodeMapper` | Singleton |
| `IValidateOptions<GranitRateLimitingOptions>` | `GranitRateLimitingOptionsValidator` | Singleton |

`IRateLimitCounterStore` et `IRateLimitQuotaProvider` sont `Scoped` pour
s'aligner avec `ICurrentTenant` et `ICurrentUserService`.

## Sécurité

- Les clés Redis ne contiennent **aucune donnée personnelle** — uniquement le
  tenant ID (GUID), le préfixe et le nom de la policy.
- Les compteurs expirent automatiquement via `PEXPIRE` — aucune accumulation
  de données dans Redis.
- Le bypass par rôle est **loggué** (audit trail) avec l'identifiant utilisateur.
- L'exception `RateLimitExceededException` n'expose pas d'information sensible
  dans le message d'erreur.

## Dépendances Granit

| Direction | Modules |
| --- | --- |
| **Dépend de** | `Granit.Core`, `Granit.ExceptionHandling`, `Granit.Features`, `Granit.Security` |
| **Utilisé par** | Module feuille (consommé par les applications) |

> Voir le [graphe de dépendances complet](../dependencies.md).
