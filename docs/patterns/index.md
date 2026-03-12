# Catalogue des Design Patterns — Granit

Ce répertoire documente les **51 patterns de conception** identifiés dans le framework
Granit. Chaque fichier décrit le pattern, son implémentation concrète dans le code source,
un diagramme Mermaid et un exemple d'usage.

## Patterns d'architecture

| Pattern | Fichier | Description |
| --- | --- | --- |
| Système de modules | [module-system.md](architecture/module-system.md) | Chargement topologique ABP-inspired avec `[DependsOn]` |
| Architecture hexagonale | [hexagonal-architecture.md](architecture/hexagonal-architecture.md) | Ports & Adapters pour découplage infrastructure |
| Architecture en couches | [layered-architecture.md](architecture/layered-architecture.md) | Séparation Domain / Application / Infrastructure |
| Pipeline Middleware | [middleware-pipeline.md](architecture/middleware-pipeline.md) | Double pipeline ASP.NET Core + Wolverine |
| Architecture événementielle | [event-driven.md](architecture/event-driven.md) | IDomainEvent (local) + IIntegrationEvent (durable) |
| REPR (Request-Endpoint-Response) | [repr.md](architecture/repr.md) | Minimal API REPR-inspired, groupement par feature, zéro dépendance |
| CQRS | [cqrs.md](architecture/cqrs.md) | Séparation IReader / IWriter, enforcement ArchUnitNET |
| Anti-Corruption Layer | [anti-corruption-layer.md](architecture/anti-corruption-layer.md) | Isolation Keycloak, S3, Brevo, FCM via DTOs internes |

## Patterns Cloud / SaaS

| Pattern | Fichier | Description |
| --- | --- | --- |
| Multi-tenancy | [multi-tenancy.md](cloud-saas/multi-tenancy.md) | 3 stratégies d'isolation, soft dependency, propagation async |
| Feature Flags | [feature-flags.md](cloud-saas/feature-flags.md) | Résolution multi-niveaux Tenant → Plan → Default + HybridCache |
| Outbox transactionnel | [transactional-outbox.md](cloud-saas/transactional-outbox.md) | Publication atomique via Wolverine Outbox |
| Idempotence | [idempotency.md](cloud-saas/idempotency.md) | Idempotence HTTP style Stripe avec state machine |
| Pre-Signed URL | [pre-signed-url.md](cloud-saas/pre-signed-url.md) | Upload/Download direct-to-cloud S3 |
| Sidecar / Behavior | [sidecar-behavior.md](cloud-saas/sidecar-behavior.md) | Propagation de contexte via Wolverine Behaviors |
| Circuit Breaker & Retry | [circuit-breaker-retry.md](cloud-saas/circuit-breaker-retry.md) | AddStandardResilienceHandler + Wolverine RetryWithCooldown |
| Cache-Aside | [cache-aside.md](cloud-saas/cache-aside.md) | Double-check locking + HybridCache L1/L2 |
| Rate Limiting / Throttling | [rate-limiting.md](cloud-saas/rate-limiting.md) | Per-tenant rate limiting Redis + quotas dynamiques Granit.Features |
| Saga / Process Manager | [saga-process-manager.md](cloud-saas/saga-process-manager.md) | GdprExportSaga, Import/Export orchestrators, WorkflowManager FSM |
| Fan-Out | [fan-out.md](cloud-saas/fan-out.md) | Wolverine cascade IEnumerable pour notifications et webhooks |
| Claim Check | [claim-check.md](cloud-saas/claim-check.md) | Soft dependency IClaimCheckStore pour payloads volumineux |
| Bulkhead Isolation | [bulkhead-isolation.md](cloud-saas/bulkhead-isolation.md) | Isolation par queues, parallélisme, circuit breaker et quotas tenant |

## Patterns GoF — Création

| Pattern | Fichier | Description |
| --- | --- | --- |
| Factory Method | [factory-method.md](gof-creation/factory-method.md) | VaultClientFactory, DbContext tenant factories |
| Singleton | [singleton.md](gof-creation/singleton.md) | AsyncLocal singletons, NullTenantContext.Instance |
| Builder | [builder.md](gof-creation/builder.md) | Fluent `AddGranit*()` extensions |

## Patterns GoF — Structure

| Pattern | Fichier | Description |
| --- | --- | --- |
| Adapter | [adapter.md](gof-structure/adapter.md) | TypedKeyCacheServiceAdapter, S3BlobClient |
| Decorator | [decorator.md](gof-structure/decorator.md) | DistributedCacheService, CachedLocalizationOverrideStore |
| Proxy | [proxy.md](gof-structure/proxy.md) | FilterProxy pour EF Core, Interceptors |
| Facade | [facade.md](gof-structure/facade.md) | DefaultBlobStorage, GranitExceptionHandler |
| Composite | [composite.md](gof-structure/composite.md) | Hiérarchie d'entités auditables |

## Patterns GoF — Comportement

| Pattern | Fichier | Description |
| --- | --- | --- |
| Strategy | [strategy.md](gof-behavior/strategy.md) | TenantIsolationStrategy, IBlobKeyStrategy, IStringEncryptionProvider |
| Chain of Responsibility | [chain-of-responsibility.md](gof-behavior/chain-of-responsibility.md) | TenantResolverPipeline, validation blobs |
| Command | [command.md](gof-behavior/command.md) | SendWebhookCommand, RunMigrationBatchCommand |
| Template Method | [template-method.md](gof-behavior/template-method.md) | GranitModule lifecycle, GranitValidator |
| State Machine | [state-machine.md](gof-behavior/state-machine.md) | IdempotencyState, BlobStatus |
| Observer / Event | [observer-event.md](gof-behavior/observer-event.md) | Wolverine event subscription implicite |
| Mediator | [mediator.md](gof-behavior/mediator.md) | Wolverine message bus |
| Null Object | [null-object.md](gof-behavior/null-object.md) | NullTenantContext, NullCacheValueEncryptor |

## Patterns de données

| Pattern | Fichier | Description |
| --- | --- | --- |
| Repository | [repository.md](data/repository.md) | Store interfaces + implémentations EF Core / InMemory |
| Soft Delete | [soft-delete.md](data/soft-delete.md) | ISoftDeletable + SoftDeleteInterceptor (RGPD) |
| Data Filtering | [data-filtering.md](data/data-filtering.md) | IDataFilter avec ImmutableDictionary AsyncLocal |
| Unit of Work | [unit-of-work.md](data/unit-of-work.md) | DbContext implicite + chaîne d'intercepteurs |
| Specification | [specification.md](data/specification.md) | QueryDefinition whitelist-first, expression trees |

## Patterns de sécurité

| Pattern | Fichier | Description |
| --- | --- | --- |
| Guard Clause | [guard-clause.md](security/guard-clause.md) | Fail-fast systématique, exceptions sémantiques |
| Claims-Based Identity | [claims-based-identity.md](security/claims-based-identity.md) | JWT Keycloak + RBAC dynamique |

## Patterns de concurrence

| Pattern | Fichier | Description |
| --- | --- | --- |
| Scope / Context Manager | [scope-context-manager.md](concurrency/scope-context-manager.md) | `using` pattern pour restauration de contexte |
| Copy-on-Write | [copy-on-write.md](concurrency/copy-on-write.md) | ImmutableDictionary pour état thread-safe |
| Double-Check Locking | [double-check-locking.md](concurrency/double-check-locking.md) | Anti-stampede sur cache miss |

## Patterns .NET idiomatiques

| Pattern | Fichier | Description |
| --- | --- | --- |
| Expression Trees | [expression-trees.md](dotnet/expression-trees.md) | Construction dynamique de query filters EF Core |
| Marker Interface | [marker-interface.md](dotnet/marker-interface.md) | ISoftDeletable, IMultiTenant, IDomainEvent |
| Options Pattern | [options-pattern.md](dotnet/options-pattern.md) | 93 classes Options, ValidateOnStart, IValidateOptions |

## Variantes maison

| Document | Fichier | Description |
| --- | --- | --- |
| Variantes maison | [variantes-maison.md](variantes-maison.md) | 10 patterns hybrides uniques à Granit |

## Voir aussi

- [Graphe de dépendances inter-modules](../framework/dependencies.md) — diagramme Mermaid
  des couplages entre packages Granit
