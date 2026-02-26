# Variantes maison — Patterns hybrides de Granit

## Définition

Certains patterns implémentés dans Granit sont des variantes ou des hybrides
de patterns classiques, adaptés aux contraintes spécifiques du framework
(HDS/RGPD, multi-tenancy, Wolverine). Ce document catalogue les 10
principales variantes « maison ».

## 1. Singleton AsyncLocal (contexte thread-safe)

**Pattern classique** : Singleton avec instance unique globale.

**Variante Granit** : `static readonly AsyncLocal<T>` — un état singleton
*par flux async*, thread-safe sans verrou.

```csharp
// src/Granit.MultiTenancy/CurrentTenant.cs
private static readonly AsyncLocal<TenantInfo?> _current = new();
```

**Nuance** : l'état est hérité par les flux enfants (`Task.Run`) mais
modifiable indépendamment grâce au copy-on-write.

## 2. ImmutableDictionary Copy-on-Write

**Pattern classique** : Copy-on-Write sur des structures de données.

**Variante Granit** : `AsyncLocal<ImmutableDictionary<Type, bool>>` empêche
les mutations d'un flux enfant de remonter vers le flux parent.

```csharp
// src/Granit.Core/DataFiltering/DataFilter.cs
_state.Value = _state.Value!.SetItem(typeof(TFilter), false);
// → Nouveau dict, l'ancien reste intact
```

**Nuance** : résout spécifiquement le « piège AsyncLocal » où un
`Dictionary<T>` partagé verrait les mutations enfant affecter le parent.

## 3. Null Object Soft Dependency

**Pattern classique** : Null Object avec une seule interface.

**Variante Granit** : `NullTenantContext` est le **défaut DI** remplacé
uniquement quand `Granit.MultiTenancy` est installé. Tous les modules
accèdent à `ICurrentTenant` via `Granit.Core.MultiTenancy` sans dépendance
directe vers `Granit.MultiTenancy`.

**Nuance** : le Null Object est architecturalement un mécanisme de
**soft dependency** — un package optionnel ne casse pas les packages qui
en dépendent implicitement.

## 4. Enum-based Strategy Selection

**Pattern classique** : Strategy avec interface + factory.

**Variante Granit** : `TenantIsolationStrategy` est un simple enum. La
factory utilise un switch expression pour sélectionner l'implémentation.

```csharp
TenantIsolationStrategy.SharedDatabase => new SharedDatabaseDbContextFactory(...),
TenantIsolationStrategy.SchemaPerTenant => new TenantPerSchemaDbContextFactory(...),
TenantIsolationStrategy.DatabasePerTenant => new TenantPerDatabaseDbContextFactory(...),
```

**Nuance** : plus simple qu'une factory abstraite quand le nombre de
stratégies est fini et connu à la compilation.

## 5. Property-based Proxy pour EF Core

**Pattern classique** : Proxy avec délégation de méthodes.

**Variante Granit** : `FilterProxy` expose des **propriétés** (pas des
méthodes) parce qu'EF Core ne peut traduire que des accès de propriétés
dans les query filters.

```csharp
// src/Granit.Persistence/Extensions/ModelBuilderExtensions.cs
internal sealed class FilterProxy(IDataFilter? dataFilter, ICurrentTenant? tenant)
{
    public bool SoftDeleteEnabled => dataFilter?.IsEnabled<ISoftDeletable>() ?? true;
}
```

**Nuance** : contournement d'une limitation EF Core, invisible pour le
développeur applicatif.

## 6. Dual Sync/Async Template Method

**Pattern classique** : Template Method avec hooks abstraits.

**Variante Granit** : `GranitModule` expose des paires sync/async.
La version async délègue à la version sync par défaut.

```csharp
// Un module peut surcharger l'un OU l'autre — pas les deux obligatoirement
public virtual Task ConfigureServicesAsync(ServiceConfigurationContext context)
{
    ConfigureServices(context);
    return Task.CompletedTask;
}
```

**Nuance** : évite de forcer l'async sur les modules simples tout en
permettant l'async pour les modules qui en ont besoin (ex : appel Vault).

## 7. Implicit Outbox via Handler Return Type

**Pattern classique** : Outbox transactionnel explicite.

**Variante Granit** (Wolverine natif) : un handler qui retourne
`IEnumerable<T>` produit automatiquement des messages Outbox.

```csharp
public static IEnumerable<object> Handle(CreatePatientCommand cmd, AppDbContext db)
{
    db.Patients.Add(new Patient { ... });
    yield return new PatientCreatedOccurred { ... };  // → local queue
    yield return new SendWelcomeEmailCommand { ... }; // → Outbox
}
```

**Nuance** : le handler est pur et déclaratif. L'infra (Outbox, routing)
est entièrement gérée par Wolverine.

## 8. Zero-allocation Streaming Hash

**Pattern classique** : SHA-256 sur un buffer complet.

**Variante Granit** : `IncrementalHash.CreateHash(SHA256)` +
`ArrayPool<byte>.Shared` dans le middleware d'idempotence.

```
src/Granit.Idempotency/Internal/IdempotencyMiddleware.cs
```

**Nuance** : le body HTTP est hashé en streaming sans allocation de buffer
complet, crucial pour les requêtes volumineuses (uploads).

## 9. Multi-tenant Composite Key (idempotence)

**Pattern classique** : clé d'idempotence = header client.

**Variante Granit** : la clé Redis inclut `{tenantId}:{userId}:{key}`,
garantissant l'isolation multi-tenant et multi-utilisateur.

**Nuance** : deux tenants différents peuvent utiliser la même clé
d'idempotence sans collision. Un même utilisateur avec deux clients ne
peut pas rejouer la requête d'un autre.

## 10. Expression Tree Query Filters

**Pattern classique** : query filters EF Core statiques.

**Variante Granit** : `ApplyGranitConventions()` construit dynamiquement
une expression combinée via `Expression.AndAlso()` pour chaque entité.

```
src/Granit.Persistence/Extensions/ModelBuilderExtensions.cs (lignes 84-126)
```

**Nuance** : résout le problème d'EF Core qui écrase les query filters
précédents lors d'appels multiples à `HasQueryFilter()`. L'expression
unique combine `ISoftDeletable` + `IActive` + `IMultiTenant`.
