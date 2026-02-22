# MultiTenancy

`DigitalDynamics.Foundation.MultiTenancy` fournit la gestion du tenant courant avec
résolution depuis le header HTTP ou le claim JWT, contexte `AsyncLocal` et middleware
ASP.NET Core pour les applications multi-tenant.

## Installation

```bash
dotnet add package DigitalDynamics.Foundation.MultiTenancy
```

## Configuration rapide

```csharp
[DependsOn(typeof(FoundationMultiTenancyModule))]
public sealed class AppModule : FoundationModule { }
```

```csharp
// Program.cs — ordre obligatoire dans le pipeline
app.UseAuthentication();
app.UseFoundationMultiTenancy();   // ← après Authentication, avant Authorization
app.UseAuthorization();
```

## appsettings.json

```json
{
  "MultiTenancy": {
    "IsEnabled": true,
    "TenantIdHeaderName": "X-Tenant-Id",
    "TenantIdClaimType": "tenant_id"
  }
}
```

## Résolveurs de tenant

La résolution suit une **chaîne ordonnée** : le premier résolveur non-null gagne.

| Résolveur | Order | Source | Configurable |
| --- | --- | --- | --- |
| `HeaderTenantResolver` | 100 | Header HTTP `X-Tenant-Id` | `TenantIdHeaderName` |
| `JwtClaimTenantResolver` | 200 | Claim JWT `tenant_id` | `TenantIdClaimType` |

Pour ajouter un résolveur personnalisé :

```csharp
// Implémente ITenantResolver avec un Order < 100 pour le rendre prioritaire
public sealed class SubdomainTenantResolver : ITenantResolver
{
    public int Order => 50;

    public Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken ct)
    {
        // Extraire le tenant depuis le sous-domaine
    }
}

// Enregistrement dans le DI
services.AddSingleton<ITenantResolver, SubdomainTenantResolver>();
```

## ICurrentTenant

Accès au tenant courant depuis n'importe quel service :

```csharp
public sealed class PatientService(ICurrentTenant currentTenant)
{
    public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken ct)
    {
        if (!currentTenant.IsAvailable)
            throw new InvalidOperationException("Contexte tenant requis.");

        return await _repo.GetByTenantAsync(currentTenant.Id!.Value, ct);
    }
}
```

### Surcharge temporaire (background jobs, tests)

```csharp
Guid targetTenantId = Guid.Parse("...");

using (currentTenant.Change(targetTenantId, "Acme"))
{
    // Dans ce scope, currentTenant.Id == targetTenantId
    await ProcessTenantDataAsync(ct);
}
// Hors du scope, le tenant précédent est restauré automatiquement
```

La surcharge est basée sur `AsyncLocal` : elle est isolée par flux asynchrone et
n'affecte pas les autres requêtes concurrentes.

### Scopes imbriqués

```csharp
using (currentTenant.Change(tenantA))
{
    // tenantA actif
    using (currentTenant.Change(tenantB))
    {
        // tenantB actif
    }
    // tenantA restauré
}
```

## Options

### `MultiTenancyOptions` (section `MultiTenancy`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `IsEnabled` | `bool` | `true` | Active/désactive la résolution du tenant |
| `TenantIdHeaderName` | `string` | `"X-Tenant-Id"` | Nom du header HTTP |
| `TenantIdClaimType` | `string` | `"tenant_id"` | Type du claim JWT |

## Architecture

```text
src/DigitalDynamics.Foundation.MultiTenancy/
├── ICurrentTenant.cs                       (interface publique)
├── TenantInfo.cs                           (record : Id, Name, Identifier)
├── CurrentTenant.cs                        (AsyncLocal, Singleton)
├── MultiTenancyOptions.cs
├── Resolvers/
│   ├── ITenantResolver.cs                  (Task<TenantInfo?> ResolveAsync)
│   ├── HeaderTenantResolver.cs             (order=100)
│   └── JwtClaimTenantResolver.cs           (order=200)
├── Pipeline/
│   └── TenantResolverPipeline.cs           (chaîne ordonnée, first non-null wins)
├── Middleware/
│   └── TenantResolutionMiddleware.cs       (résout + stocke dans ICurrentTenant)
├── FoundationMultiTenancyModule.cs
└── Extensions/
    ├── MultiTenancyServiceCollectionExtensions.cs
    └── MultiTenancyApplicationBuilderExtensions.cs  (UseFoundationMultiTenancy)
```

## Services enregistrés

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `ICurrentTenant` | `CurrentTenant` | Singleton |
| `ITenantResolver` | `HeaderTenantResolver` | Singleton |
| `ITenantResolver` | `JwtClaimTenantResolver` | Singleton |
| `TenantResolverPipeline` | `TenantResolverPipeline` | Singleton |
| `TenantResolutionMiddleware` | `TenantResolutionMiddleware` | Scoped |

## Sécurité RGPD

- `TenantInfo.Id` (GUID) est une **pseudonymisation** — ne jamais stocker de nom
  ou email dans `ProviderKey` (utilisé par Foundation.Settings)
- Le middleware est positionné après `UseAuthentication()` : le claim JWT est
  disponible lors de la résolution par `JwtClaimTenantResolver`
- `IsEnabled = false` désactive le middleware sans décharger le module (utile
  pour les environnements de test sans multi-tenancy)
