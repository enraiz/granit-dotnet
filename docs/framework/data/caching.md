# Caching

`Granit.Caching` fournit une abstraction de cache typée
(`ICacheService<T>`) au-dessus de `IDistributedCache` avec trois fournisseurs
interchangeables, une protection stampede sans fuite mémoire, et un chiffrement
AES-256 opt-in pour la conformité RGPD/ISO 27001.

## Fournisseurs disponibles

| Package | Fournisseur | Dépend de | Quand l'utiliser |
| --- | --- | --- | --- |
| `Granit.Caching` | Memory (défaut) | — | Développement, tests locaux |
| `Granit.Caching.StackExchangeRedis` | Redis | `Caching` | Production simple |
| `Granit.Caching.Hybrid` | L1 Memory + L2 Redis | `Caching` + `StackExchangeRedis` | Production Kubernetes multi-pods |

`Caching.Hybrid` utilise `IDistributedCache` (Redis) comme couche L2. Il dépend donc
de `Caching.StackExchangeRedis` en tant que **dépendance transitive** : installer
`Caching.Hybrid` suffit, `StackExchangeRedis` est tiré automatiquement.

## Installation

```bash
# Développement — Memory seul
dotnet add package Granit.Caching

# Production simple — Redis (inclut Caching en transitif)
dotnet add package Granit.Caching.StackExchangeRedis

# Production Kubernetes — Hybrid L1+L2 (inclut Caching + StackExchangeRedis en transitif)
dotnet add package Granit.Caching.Hybrid
```

## Configuration rapide

### Dev — Memory seul

```csharp
[DependsOn(typeof(GranitCachingModule))]
public sealed class AppModule : GranitModule { }
```

### Production simple — Redis + chiffrement AES

```csharp
[DependsOn(typeof(GranitCachingRedisModule))]
public sealed class AppModule : GranitModule { }
```

### Production Kubernetes — HybridCache L1+L2 + chiffrement AES

```csharp
[DependsOn(typeof(GranitCachingHybridModule))]
public sealed class AppModule : GranitModule { }
```

## appsettings.json

### Dev (Memory, sans chiffrement)

```json
{
  "Cache": {
    "KeyPrefix": "myapp"
  }
}
```

### Production (Redis + chiffrement ISO 27001)

```json
{
  "Cache": {
    "KeyPrefix": "myapp",
    "DefaultAbsoluteExpirationRelativeToNow": "01:00:00",
    "DefaultSlidingExpiration": "00:20:00",
    "EncryptValues": true,
    "Encryption": {
      "Key": "<base64-aes256-key-from-vault>"
    },
    "Redis": {
      "IsEnabled": true,
      "Configuration": "redis-service:6379",
      "InstanceName": "myapp:"
    }
  }
}
```

### Production Kubernetes (HybridCache)

```json
{
  "Cache": {
    "KeyPrefix": "myapp",
    "EncryptValues": true,
    "Encryption": {
      "Key": "<base64-aes256-key-from-vault>"
    },
    "Redis": {
      "IsEnabled": true,
      "Configuration": "redis-service:6379",
      "InstanceName": "myapp:"
    },
    "Hybrid": {
      "LocalCacheExpiration": "00:00:30"
    }
  }
}
```

## Utilisation

### Clé string (simple)

```csharp
public sealed class UserService(ICacheService<UserCacheItem> cache)
{
    public async Task<UserCacheItem> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await cache.GetOrAddAsync(
            id.ToString(),
            async ct => await _repo.GetByIdAsync(id, ct),
            cancellationToken: ct);
    }
}
```

### Clé typée (ABP-style)

```csharp
public sealed class UserService(ICacheService<UserCacheItem, Guid> cache)
{
    public async Task<UserCacheItem> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await cache.GetOrAddAsync(id,
            async ct => await _repo.GetByIdAsync(id, ct),
            cancellationToken: ct);
    }
}
```

Le code métier est **identique** quel que soit le fournisseur (Memory, Redis ou Hybrid).

## Convention de nommage des clés

Les clés sont construites automatiquement selon le format :

```text
{KeyPrefix}:{CacheName}:{userKey}
```

Exemples :

| `KeyPrefix` | `TCacheItem` | `userKey` | Clé finale |
| --- | --- | --- | --- |
| `myapp` | `UserCacheItem` | `d4e5f6` | `myapp:User:d4e5f6` |
| `myapp` | `PatientCacheItem` | `p-001` | `myapp:Patient:p-001` |
| `dd` | `[CacheName("Session")]SessionData` | `abc` | `dd:Session:abc` |

La convention supprime automatiquement le suffixe `CacheItem` du nom du type.
L'attribut `[CacheName("nom")]` permet de surcharger la convention.

## Chiffrement RGPD/ISO 27001

### Principe

Redis stocke les données en clair. Pour les données sensibles (données
nominatives, données personnelles), le chiffrement AES-256-CBC est activé via `ICacheValueEncryptor`.

### Activation globale

```json
{
  "Cache": {
    "EncryptValues": true,
    "Encryption": {
      "Key": "<clé AES-256 base64 depuis Vault>"
    }
  }
}
```

### Activation par type (opt-in/opt-out)

L'attribut `[CacheEncrypted]` permet un contrôle fin par type, indépendamment
du flag global `EncryptValues` :

```csharp
// Toujours chiffrer, même si EncryptValues = false
[CacheEncrypted]
public sealed class PatientCacheItem { ... }

// Jamais chiffrer, même si EncryptValues = true
[CacheEncrypted(false)]
public sealed class PublicConfigCacheItem { ... }

// Suit le flag global CachingOptions.EncryptValues
public sealed class UserSessionCacheItem { ... }
```

**Priorité** : attribut `[CacheEncrypted]` > flag global `EncryptValues`.

### Implémentation AES

- Algorithme : AES-256-CBC avec PKCS7 padding
- IV : aléatoire par opération (`RandomNumberGenerator.Fill`)
- Format stocké : `[16 octets IV][N octets CipherText]`
- Clé : fournie via Vault (jamais en clair dans la config)

## Protection stampede

`GetOrAddAsync` garantit que la factory n'est exécutée **qu'une seule fois**
même sous forte concurrence (10+ requêtes simultanées sur la même clé absente) :

```text
1. Vérification rapide sans verrou (cache hit → retour immédiat)
2. Acquisition du verrou (SemaphoreSlim dans IMemoryCache, TTL 30 s)
3. Double-check locking (un autre thread a peut-être rempli le cache)
4. Exécution de la factory (une seule fois garantie)
```

Les `SemaphoreSlim` sont stockés dans un `IMemoryCache` dédié (clé DI :
`Granit.Caching.Locks`, `SizeLimit = 10 000`) et non
dans un `ConcurrentDictionary` non borné : le GC nettoie automatiquement
les verrous inutilisés après 30 secondes.

## Fournisseur Hybrid (Kubernetes)

### Architecture L1+L2

```text
Pod A — GetOrAddAsync("patient:p-001") :
  L1 miss → L2 (Redis) miss → DB → écrit L1 (30 s) + L2 (1 h)

Pod A — requête suivante :
  L1 hit → < 1 ms

Pod B — même requête :
  L1 miss → L2 (Redis) hit → ~2 ms

RemoveAsync sur Pod A :
  efface L2 + L1 du Pod A
  Pod B : son L1 expire au plus dans 30 s (LocalCacheExpiration)
```

### Invalidation inter-pods

`HybridCache` .NET 9 n'inclut pas de Pub/Sub pour invalider les L1 distants.
La fenêtre de données obsolètes est bornée par `LocalCacheExpiration` :

- Valeur recommandée : ≤ 60 s
- Valeur par défaut : **30 s**

Pour les données à forte cohérence, utiliser le fournisseur Redis pur.

### Protection stampede Hybrid

Native dans `HybridCache` — aucun `SemaphoreSlim` supplémentaire nécessaire.

## Options de configuration

### `CachingOptions` (section `Cache`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `KeyPrefix` | `string` | `"dd"` | Préfixe de toutes les clés |
| `DefaultAbsoluteExpirationRelativeToNow` | `TimeSpan?` | `1h` | Expiration absolue par défaut |
| `DefaultSlidingExpiration` | `TimeSpan?` | `20min` | Expiration glissante par défaut |
| `EncryptValues` | `bool` | `false` | Chiffrement AES global |

### `CacheEncryptionOptions` (section `Cache:Encryption`)

| Propriété | Type | Description |
| --- | --- | --- |
| `Key` | `string` | Clé AES-256 en base64 (32 octets) — depuis Vault |

### `RedisCachingOptions` (section `Cache:Redis`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `IsEnabled` | `bool` | `true` | Désactiver Redis sans changer le module chargé |
| `Configuration` | `string` | `"localhost:6379"` | Chaîne de connexion StackExchange.Redis |
| `InstanceName` | `string` | `"dd:"` | Préfixe des clés Redis (isolation multi-app) |

### `HybridCachingOptions` (section `Cache:Hybrid`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `LocalCacheExpiration` | `TimeSpan` | `30s` | Expiration du L1 local (borne la staleness inter-pods) |

## Architecture

```text
src/
  Granit.Caching/
  ├── ICacheService.cs                      (clé string)
  ├── ICacheServiceOfTKey.cs                (clé typée, ABP-style)
  ├── ICacheValueEncryptor.cs               (interface chiffrement)
  ├── NullCacheValueEncryptor.cs            (no-op, dev/Memory)
  ├── AesCacheValueEncryptor.cs             (AES-256-CBC, production)
  ├── CacheNameAttribute.cs                 ([CacheName("...")])
  ├── CacheEncryptedAttribute.cs            ([CacheEncrypted], [CacheEncrypted(false)])
  ├── CacheNameProvider.cs                  (convention "UserCacheItem" → "User")
  ├── CacheEncryptionResolver.cs            (résolution attribut vs flag global)
  ├── DistributedCacheService.cs            (implémentation Memory/Redis)
  ├── TypedKeyCacheServiceAdapter.cs        (adaptateur clé typée → string)
  ├── CachingOptions.cs
  ├── CacheEncryptionOptions.cs
  ├── GranitCachingModule.cs
  └── Extensions/
      └── CachingServiceCollectionExtensions.cs

  Granit.Caching.StackExchangeRedis/
  ├── RedisCachingOptions.cs
  ├── GranitCachingRedisModule.cs
  └── Extensions/
      └── RedisCachingServiceCollectionExtensions.cs

  Granit.Caching.Hybrid/
  ├── HybridCachingOptions.cs
  ├── HybridCacheService.cs
  ├── GranitCachingHybridModule.cs
  └── Extensions/
      └── HybridCachingServiceCollectionExtensions.cs
```

## Services enregistrés

### `GranitCachingModule`

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IDistributedCache` | `MemoryDistributedCache` | Singleton |
| `IMemoryCache` (keyed: Locks) | `MemoryCache` (SizeLimit=10000) | Singleton |
| `ICacheValueEncryptor` | `NullCacheValueEncryptor` | Singleton |
| `ICacheService<T>` | `DistributedCacheService<T>` | Singleton |
| `ICacheService<T, TKey>` | `TypedKeyCacheServiceAdapter<T, TKey>` | Singleton |

### `GranitCachingRedisModule` (surcharge Memory)

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IDistributedCache` | `RedisCache` | Singleton |
| `ICacheValueEncryptor` | `AesCacheValueEncryptor` (si `EncryptValues=true`) | Singleton |

### `GranitCachingHybridModule` (surcharge Redis)

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `HybridCache` | (natif .NET 9) | Singleton |
| `ICacheService<T>` | `HybridCacheService<T>` | Singleton |

## Sécurité

- La clé AES (`CacheEncryptionOptions.Key`) doit être fournie via **Vault**,
  jamais en clair dans `appsettings.json`
- L'IV est aléatoire par opération : deux chiffrements du même plaintext
  produisent des ciphertexts différents (protection contre les attaques par analyse)
- `IsEnabled = false` permet de désactiver Redis en développement sans modifier
  le module chargé (utile pour les environnements sans Redis)
- Le L1 HybridCache est en mémoire locale du pod : pas de données sensibles
  exposées sur le réseau lors des accès L1

## Dépendances Granit

| Package | Dépend de | Utilisé par |
| --- | --- | --- |
| `Granit.Caching` | `Granit.Core` | `Authorization`, `Settings`, `Idempotency`, `Features`, `Caching.StackExchangeRedis`, `Caching.Hybrid` |
| `Granit.Caching.StackExchangeRedis` | `Caching` | `Caching.Hybrid` |
| `Granit.Caching.Hybrid` | `Caching`, `Caching.StackExchangeRedis`, `Timing` | Module feuille (opt-in applicatif) |

> Voir le [graphe de dépendances complet](../dependencies.md).
