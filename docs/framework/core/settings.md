# Settings

`Granit.Settings` fournit un système de paramètres dynamiques
avec résolution en cascade `User → Tenant → Global → Configuration → Default`,
cache intégré et chiffrement optionnel pour les paramètres sensibles.

## Installation

```bash
dotnet add package Granit.Settings
```

Pour la persistance en base de données (production) :

```bash
dotnet add package Granit.Settings.EntityFrameworkCore
```

## Configuration rapide

```csharp
[DependsOn(typeof(GranitSettingsModule))]
public sealed class AppModule : GranitModule { }
```

## Déclarer des paramètres

Les paramètres sont déclarés via `ISettingDefinitionProvider`. Créer un provider
par module fonctionnel :

```csharp
public sealed class AppSettingDefinitionProvider : ISettingDefinitionProvider
{
    public void Define(ISettingDefinitionContext context)
    {
        context.Add(new SettingDefinition("App.Theme")
        {
            DefaultValue = "dark",
            DisplayName = "Thème de l'interface",
            IsVisibleToClients = true
        });

        context.Add(new SettingDefinition("App.ApiKey")
        {
            IsEncrypted = true,               // chiffré en base, plaintext en cache
            DisplayName = "Clé d'API externe",
            IsVisibleToClients = false
        });

        context.Add(new SettingDefinition("App.MaxUploadMb")
        {
            DefaultValue = "10",
            Providers = { GlobalSettingValueProvider.ProviderName,   // G
                          TenantSettingValueProvider.ProviderName }   // T seulement
        });
    }
}
```

Enregistrement du provider :

```csharp
services.AddSingleton<ISettingDefinitionProvider, AppSettingDefinitionProvider>();
```

## Lire un paramètre

```csharp
public sealed class ThemeService(ISettingProvider settings)
{
    public async Task<string> GetThemeAsync(CancellationToken ct)
    {
        string? theme = await settings.GetOrNullAsync("App.Theme", ct);
        return theme ?? "dark";
    }
}
```

La cascade est automatique : si l'utilisateur courant a une valeur, elle est
retournée. Sinon, le tenant, puis le global, puis `appsettings.json`, puis la
valeur par défaut déclarée.

## Écrire un paramètre

```csharp
public sealed class AdminService(ISettingManager settings)
{
    // Valeur globale (tous les tenants et utilisateurs)
    public Task SetGlobalThemeAsync(string theme, CancellationToken ct) =>
        settings.SetGlobalAsync("App.Theme", theme, ct);

    // Valeur pour un tenant spécifique
    public Task SetTenantThemeAsync(Guid tenantId, string theme, CancellationToken ct) =>
        settings.SetForTenantAsync(tenantId, "App.Theme", theme, ct);

    // Valeur pour un utilisateur spécifique
    public Task SetUserThemeAsync(string userId, string theme, CancellationToken ct) =>
        settings.SetForUserAsync(userId, "App.Theme", theme, ct);
}
```

## Cascade de résolution

```text
User (U, order=100)
  ↓ null → Tenant (T, order=200)
    ↓ null → Global (G, order=300)
      ↓ null → Configuration (C, order=400) ← appsettings.json / env vars
        ↓ null → Default (D, order=500) ← SettingDefinition.DefaultValue
```

Le premier provider non-null gagne. Le paramètre `IsInherited = false` arrête
la cascade après le premier provider applicable qui retourne null.

## Surcharge via appsettings.json (provider C)

```json
{
  "Settings": {
    "App.Theme": "light",
    "App.MaxUploadMb": "50"
  }
}
```

Les valeurs de configuration ont priorité sur `DefaultValue` mais sont surchargées
par les valeurs Global, Tenant et User stockées en base.

## appsettings.json — module options

```json
{
  "Settings": {
    "CacheExpiration": "00:30:00"
  }
}
```

## SettingDefinition — propriétés

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `Name` | `string` | — | Nom unique du paramètre (clé de lookup) |
| `DefaultValue` | `string?` | `null` | Valeur retournée si aucun provider n'a de valeur |
| `IsEncrypted` | `bool` | `false` | Chiffrement au repos via `IStringEncryptionService` |
| `IsVisibleToClients` | `bool` | `false` | Exposable via API publique |
| `IsInherited` | `bool` | `true` | Cascade vers le niveau suivant si null |
| `Providers` | `IList<string>` | `[]` | Liste blanche de providers (vide = tous autorisés) |
| `DisplayName` | `string?` | `null` | Libellé UI |
| `Description` | `string?` | `null` | Description longue |

## Chiffrement HDS

Pour les paramètres sensibles (`IsEncrypted = true`) :

- **Chiffrement** appliqué uniquement à la couche `ISettingStore` (persistance)
- **Le cache stocke le plaintext** — `Granit.Caching` chiffre déjà le backend
  Redis via `AesCacheValueEncryptor`, évitant un double chiffrement coûteux
- Requiert `GranitEncryptionModule` avec une `PassPhrase` depuis Vault

```csharp
context.Add(new SettingDefinition("Integrations.FhirApiKey")
{
    IsEncrypted = true,
    IsVisibleToClients = false,
    Providers = { GlobalSettingValueProvider.ProviderName }   // global only
});
```

## Cache

Les providers store-backed (G, T, U) utilisent `ICacheService<SettingValue>` avec
invalidation automatique lors des écritures via `ISettingManager`.

Format des clés de cache : `{ProviderName}:{ProviderKey}:{SettingName}`

| Exemple | Clé cache |
| --- | --- |
| Global `App.Theme` | `G:global:App.Theme` |
| Tenant `abc123` `App.Theme` | `T:abc123:App.Theme` |
| User `user-42` `App.Theme` | `U:user-42:App.Theme` |

L'expiration est configurable via `SettingsOptions.CacheExpiration` (défaut : 30 min).

## Persistance EF Core (production)

`InMemorySettingStore` est uniquement destiné aux tests et au développement.
En production, utiliser `Granit.Settings.EntityFrameworkCore` qui persiste les
valeurs dans la base de données de l'application (zéro connexion supplémentaire).

### 1 — Module

```csharp
[DependsOn(
    typeof(GranitSettingsModule),
    typeof(GranitSettingsEntityFrameworkCoreModule))]
public sealed class AppModule : GranitModule { }
```

### 2 — DbContext hôte

Le DbContext de l'application doit implémenter `ISettingsDbContext` et appeler
`modelBuilder.ConfigureSettingsModule()` dans `OnModelCreating` :

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), ISettingsDbContext
{
    public DbSet<SettingRecord> SettingRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureSettingsModule();  // crée granit_setting_records
    }
}
```

### 3 — Enregistrement du store

```csharp
// Program.cs — remplace InMemorySettingStore par EfCoreSettingStore
builder.AddGranitSettingsEfCore<AppDbContext>();
```

Cette extension utilise `IServiceScopeFactory` pour résoudre le DbContext
depuis un Singleton (pattern standard pour consommer un service Scoped depuis
un Singleton).

### 4 — Migration

```bash
dotnet ef migrations add InitSettings \
  --project src/MyApp \
  --startup-project src/MyApp
```

La migration crée la table `granit_setting_records` avec un index unique sur
`(Name, ProviderName, ProviderKey)`.

### Schéma de la table

| Colonne | Type | Contraintes |
| --- | --- | --- |
| `Id` | `uuid` | PK |
| `Name` | `varchar(256)` | NOT NULL |
| `ProviderName` | `varchar(4)` | NOT NULL (`"G"`, `"T"`, `"U"`) |
| `ProviderKey` | `varchar(256)` | NULL (null = global) |
| `Value` | `text` | NULL |
| `CreatedAt` | `timestamptz` | NOT NULL — audit HDS |
| `CreatedBy` | `varchar(256)` | NOT NULL — audit HDS |
| `ModifiedAt` | `timestamptz` | NULL |
| `ModifiedBy` | `varchar(256)` | NULL |

Index unique : `uq_granit_setting_records_name_provider` sur `(Name, ProviderName, ProviderKey)`.

## Architecture

```text
src/Granit.Settings/
├── Definitions/
│   ├── SettingDefinition.cs                (métadonnées statiques)
│   ├── ISettingDefinitionContext.cs        (Add, GetOrNull)
│   ├── ISettingDefinitionProvider.cs       (void Define(context))
│   └── SettingDefinitionManager.cs        (Singleton, registre global)
├── Values/
│   ├── SettingValue.cs                     (record : Name, ProviderName, ProviderKey, Value)
│   └── ISettingStore.cs                    (abstraction de persistance basse couche)
├── Stores/
│   └── InMemorySettingStore.cs             (ConcurrentDictionary, dev/tests)
├── Providers/
│   ├── ISettingValueProvider.cs            (Name, Order, GetOrNull/Set/Clear)
│   ├── DefaultValueSettingValueProvider.cs (D, order=500)
│   ├── ConfigurationSettingValueProvider.cs(C, order=400, IConfiguration)
│   ├── GlobalSettingValueProvider.cs       (G, order=300, key=null)
│   ├── TenantSettingValueProvider.cs       (T, order=200, key=tenantId)
│   ├── UserSettingValueProvider.cs         (U, order=100, key=userId)
│   ├── SettingValueProviderManager.cs      (liste triée par Order)
│   └── SettingCacheKey.cs                  (convention clé cache interne)
├── Services/
│   ├── ISettingProvider.cs                 (GetOrNullAsync, GetAllAsync — lecture)
│   ├── SettingProvider.cs                  (cascade, gestion IsInherited)
│   ├── ISettingManager.cs                  (SetGlobal/Tenant/User, Delete — écriture)
│   └── SettingManager.cs                   (écrit store + invalide cache)
├── Options/
│   └── SettingsOptions.cs                  (CacheExpiration)
├── GranitSettingsModule.cs
└── Extensions/
    └── SettingsServiceCollectionExtensions.cs
```

## Services enregistrés

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `SettingDefinitionManager` | `SettingDefinitionManager` | Singleton |
| `ISettingStore` | `InMemorySettingStore` (défaut) | Singleton |
| `ISettingValueProvider` × 5 | U, T, G, C, D | Singleton |
| `SettingValueProviderManager` | `SettingValueProviderManager` | Singleton |
| `ISettingProvider` | `SettingProvider` | Scoped |
| `ISettingManager` | `SettingManager` | Scoped |

## Dépendances Granit

```text
GranitSettingsModule
  ├── GranitCachingModule      (ICacheService<SettingValue>)
  ├── GranitMultiTenancyModule (ICurrentTenant pour provider T)
  ├── GranitEncryptionModule   (IStringEncryptionService pour IsEncrypted)
  └── GranitSecurityModule     (ICurrentUserService pour provider U)
```

| Direction | Modules |
|-----------|---------|
| **Dépend de** | `Granit.Core`, `Granit.Caching`, `Granit.Encryption`, `Granit.Security` |
| **Utilisé par** | `Granit.Settings.EntityFrameworkCore` |
| **Package EF Core** | `Granit.Settings.EntityFrameworkCore` → ajoute `Granit.Persistence` |

> Voir le [graphe de dépendances complet](../dependencies.md).
