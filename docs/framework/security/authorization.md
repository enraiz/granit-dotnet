# Authorization — RBAC et gestion des permissions

Deux packages constituent la couche d'autorisation de Granit.

```text
Granit.Authorization
  ├── Abstractions : IPermissionDefinitionProvider, IPermissionChecker
  ├── Services : PermissionDefinitionManager (Singleton), PermissionChecker (Scoped)
  ├── ASP.NET Core : DynamicPermissionPolicyProvider, PermissionAuthorizationHandler
  └── DependsOn : Core, Security, MultiTenancy, Caching

Granit.Authorization.EntityFrameworkCore
  ├── Entité : PermissionGrant (AuditedEntity + IMultiTenant)
  ├── Store : EfCorePermissionGrantStore
  ├── Manager : PermissionManager (cache + audit HDS)
  └── DependsOn : Granit.Authorization, Persistence
```

## Principes fondamentaux

**RBAC strict** : les permissions sont attribuées aux **rôles**, jamais aux utilisateurs.
Qu'une application utilise Keycloak, ASP.NET Core Identity ou Auth0, `ICurrentUserService.Roles`
est la seule source de rôles. Le framework est agnostique vis-à-vis de l'IdP.

**Pas de permission creep** : interdire les grants par utilisateur élimine la dérive des droits
(un utilisateur qui accumule des permissions individuelles après des changements de service).
Si un utilisateur a besoin d'un droit spécifique, la bonne pratique est de créer un rôle précis
(ex : `auditeur_financier_temporaire`), d'y attacher la permission, et de lui assigner ce rôle.

---

## Installation

```bash
dotnet add package Granit.Authorization
dotnet add package Granit.Authorization.EntityFrameworkCore
```

---

## Configuration

### Granit.Authorization

```json
{
  "Authorization": {
    "AdminRoles": ["admin"],
    "CacheDuration": "00:05:00",
    "AlwaysAllow": false
  }
}
```

| Paramètre | Type | Défaut | Description |
| --- | --- | --- | --- |
| `AdminRoles` | `string[]` | `["admin"]` | Rôles qui bypasse tous les checks (root of trust). |
| `CacheDuration` | `TimeSpan` | `00:05:00` | Durée du cache par `(TenantId, RoleName, PermissionName)`. |
| `AlwaysAllow` | `bool` | `false` | Dev/test uniquement : accorde toutes les permissions sans DB. |

### Granit.Authorization.EntityFrameworkCore

Le DbContext de l'application hôte doit implémenter `IPermissionGrantDbContext` :

```csharp
public class AppDbContext : DbContext, IPermissionGrantDbContext
{
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigurePermissionGrants();
    }
}
```

---

## Module system

```csharp
[DependsOn(
    typeof(GranitAuthorizationEntityFrameworkCoreModule),
    // ... autres modules
)]
public sealed class AppModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddGranitAuthorizationEntityFrameworkCore<AppDbContext>();

        // Enregistrer les providers de permissions
        context.Services.AddSingleton<IPermissionDefinitionProvider, InvoicesPermissionProvider>();
    }
}
```

---

## Définir les permissions

Chaque module métier déclare ses permissions via `IPermissionDefinitionProvider`.
Plusieurs modules peuvent ajouter des permissions au même groupe (pattern GetOrAdd).

```csharp
public sealed class InvoicesPermissionProvider : IPermissionDefinitionProvider
{
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup("Invoices", "Factures");
        group.AddPermission("Invoices.Read",   "Consulter les factures");
        group.AddPermission("Invoices.Create", "Créer des factures");
        group.AddPermission("Invoices.Delete", "Supprimer des factures");
    }
}
```

Le `PermissionDefinitionManager` (Singleton) agrège tous les providers à la première utilisation.

---

## Protéger les endpoints

### Attribut `[Permission]`

```csharp
[Permission("Invoices.Delete")]
public IActionResult Delete(Guid id) { ... }

// Équivalent — tous les deux fonctionnent
[Authorize("Invoices.Delete")]
public IActionResult Delete(Guid id) { ... }
```

Le `DynamicPermissionPolicyProvider` crée la policy ASP.NET Core à la volée pour toute
permission connue dans le `PermissionDefinitionManager`. Les policies standards
(`"Authenticated"`, `"Admin"`) sont déléguées au provider par défaut.

### Injection directe

```csharp
public sealed class InvoiceService(IPermissionChecker permissionChecker)
{
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!await permissionChecker.IsGrantedAsync("Invoices.Delete", ct))
            throw new ForbiddenException("Authorization:Permission:Denied",
                "Vous n'avez pas la permission de supprimer des factures.");

        // ...
    }
}
```

---

## Flux de vérification

```text
[Permission("Invoices.Delete")]
          │
          ▼
DynamicPermissionPolicyProvider
  "Invoices.Delete" est une permission connue ?
  ├── Oui → PermissionRequirement("Invoices.Delete")
  └── Non → DefaultAuthorizationPolicyProvider (fallback)
          │
          ▼
PermissionAuthorizationHandler
  appelle IPermissionChecker.IsGrantedAsync("Invoices.Delete")
          │
          ├─ 1. AlwaysAllow = true → Accordé (dev/test)
          ├─ 2. Non authentifié   → Refusé
          ├─ 3. AdminRole bypass  → Accordé (sans DB)
          ├─ 4. Permission non définie → InvalidOperationException
          └─ 5. Pour chaque rôle du JWT :
                 cache hit → résultat
                 cache miss → IPermissionGrantStore → mise en cache 5 min
                 un seul true suffit → Accordé
                 aucun true → Refusé
```

---

## Gérer les grants (IPermissionManager)

`IPermissionManager` est le service d'administration. Il doit être protégé par
un rôle `AdminRole` ou la permission dédiée `Permissions.Manage`.

```csharp
// Accorder une permission à un rôle
await permissionManager.SetAsync("Invoices.Delete", "accountant", tenantId, isGranted: true);

// Révoquer
await permissionManager.SetAsync("Invoices.Delete", "accountant", tenantId, isGranted: false);

// Consulter les permissions d'un rôle
IReadOnlyList<string> permissions =
    await permissionManager.GetGrantedPermissionsAsync("accountant", tenantId);

// Consulter les rôles ayant accès à une permission
IReadOnlyList<string> roles =
    await permissionManager.GetGrantedRolesAsync("Invoices.Delete", tenantId);
```

**Chaque appel à `SetAsync` :**

1. Valide que la permission est définie dans `IPermissionDefinitionManager`
2. Upsert ou supprime le `PermissionGrant` en base
3. Invalide l'entrée de cache correspondante
4. Émet un log structuré `[AUDIT]` → Serilog → Loki (rétention 3 ans, obligation HDS)

```text
[AUDIT] Permission Granted: permission=Invoices.Delete role=accountant tenantId=<guid>
[AUDIT] Permission Revoked: permission=Invoices.Delete role=accountant tenantId=<guid>
```

---

## Cache

Le cache est géré automatiquement par `PermissionChecker` via `ICacheService<PermissionGrantCacheItem>`.

**Clé de cache :** `perm:{tenantId|"global"}:{roleName}:{permissionName}`

**Stratégie :** cache par rôle (et non par utilisateur) — tous les utilisateurs
partageant le même rôle partagent la même entrée de cache.

| Par utilisateur (évité) | Par rôle (retenu) |
| --- | --- |
| N utilisateurs × M permissions | K rôles × M permissions |
| 100 000 entrées (1000 users, 100 perms) | 300 entrées (3 rôles, 100 perms) |
| Invalidation complexe | Invalidation triviale : 1 clé par `SetAsync` |

**Invalidation :** `IPermissionManager.SetAsync` invalide l'entrée après chaque mutation.
Les entrées restantes expirent naturellement selon `CacheDuration` (défaut : 5 min).

---

## Migration EF Core

Après avoir implémenté `IPermissionGrantDbContext` sur le DbContext de l'application :

```bash
dotnet ef migrations add AddPermissionGrants --project src/YourApp.Persistence
dotnet ef database update
```

La table générée :

```sql
CREATE TABLE security_permission_grants (
    id           uuid           NOT NULL PRIMARY KEY,
    name         varchar(256)   NOT NULL,
    role_name    varchar(256)   NOT NULL,
    tenant_id    uuid           NULL,
    created_at   timestamptz    NOT NULL,
    created_by   text           NOT NULL,
    modified_at  timestamptz    NULL,
    modified_by  text           NULL,
    CONSTRAINT uq_security_permission_grants_tenant_name_role
        UNIQUE (tenant_id, name, role_name)
);
```

---

## Sécurité et conformité

### HDS — Audit trail

Chaque modification de grant via `IPermissionManager.SetAsync` émet un log structuré
capturé par Serilog puis routé vers Loki. La politique de rétention Loki doit être
configurée à **3 ans minimum** pour satisfaire l'obligation HDS.

### RGPD — Minimisation des données

- Aucune donnée personnelle dans les clés de cache (rôle + nom de permission uniquement)
- Aucune donnée personnelle dans les logs d'audit (rôle + nom de permission + tenantId)
- Le `UserId` n'est jamais stocké dans `PermissionGrant` (RBAC strict)

### Multi-tenant

- `TenantId = null` → grant à portée globale (host-level)
- `TenantId = <guid>` → grant isolé au tenant

L'isolation est garantie par le filtre `g.TenantId == tenantId` dans toutes les requêtes
EF Core. L'intercepteur `AuditedEntityInterceptor` injecte automatiquement le `TenantId`
depuis `ICurrentTenant` lors de la création de l'entité.

### Bootstrap / root of trust

Le ou les rôles configurés dans `AdminRoles` bypasse le `PermissionChecker`
(vérification directe via `ICurrentUserService.IsInRole`). Ces rôles ne peuvent
**pas** être révoqués via `IPermissionManager.SetAsync` — ils sont définis dans
la configuration et déployés hors du périmètre applicatif (Keycloak, Vault, etc.).

## Dépendances Granit

| Direction | Modules |
|-----------|---------|
| **Dépend de** | `Granit.Core`, `Granit.Security`, `Granit.Caching` |
| **Utilisé par** | `Granit.Authorization.EntityFrameworkCore`, `Granit.BackgroundJobs.Endpoints`, `Granit.Localization.Endpoints` |
| **Package EF Core** | `Granit.Authorization.EntityFrameworkCore` → ajoute `Granit.Persistence` |

> Voir le [graphe de dépendances complet](../dependencies.md).
