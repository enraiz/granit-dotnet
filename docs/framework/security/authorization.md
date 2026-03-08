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

Les `DisplayName` des groupes et permissions sont des `LocalizableString` : ils sont
résolus à la volée par `IStringLocalizerFactory` selon la culture de la requête HTTP.
Chaque module fournit ses propres fichiers JSON de localisation (7 langues obligatoires).

```csharp
// 1. Marker class pour la localisation (auto-discovery par convention)
[LocalizationResourceName("InvoicesEndpoints")]
internal sealed class InvoicesEndpointsLocalizationResource;

// 2. Provider de permissions
public sealed class InvoicesPermissionProvider : IPermissionDefinitionProvider
{
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup("Invoices",
            LocalizableString.Create<InvoicesEndpointsLocalizationResource>(
                "PermissionGroup:Invoices"));

        group.AddPermission("Invoices.Read",
            LocalizableString.Create<InvoicesEndpointsLocalizationResource>(
                "Permission:Invoices.Read"));

        group.AddPermission("Invoices.Create",
            LocalizableString.Create<InvoicesEndpointsLocalizationResource>(
                "Permission:Invoices.Create"));

        group.AddPermission("Invoices.Delete",
            LocalizableString.Create<InvoicesEndpointsLocalizationResource>(
                "Permission:Invoices.Delete"));
    }
}
```

Fichiers JSON (7 cultures : `en`, `fr`, `nl`, `de`, `es`, `it`, `pt`) sous
`Localization/InvoicesEndpoints/{culture}.json`, déclarés en `<EmbeddedResource>` :

```json
{
  "culture": "fr",
  "texts": {
    "PermissionGroup:Invoices": "Factures",
    "Permission:Invoices.Read": "Consulter les factures",
    "Permission:Invoices.Create": "Créer des factures",
    "Permission:Invoices.Delete": "Supprimer des factures"
  }
}
```

Pour les tests ou un usage sans localisation, `LocalizableString.Fixed("texte")`
retourne une valeur fixe sans résolution i18n.

Le `PermissionDefinitionManager` (Singleton) agrège tous les providers à la première utilisation.

---

## Convention de nommage des permissions

Chaque permission suit le format **`[Module].[Ressource].[Action]`** :

| Segment | Description | Exemples |
| --- | --- | --- |
| **Module** | Le contexte fonctionnel (package Granit ou module métier) | `Authorization`, `DataExchange`, `Invoices` |
| **Ressource** | L'entité ou le concept manipulé, au **pluriel** | `Grants`, `Imports`, `Entries`, `Patients` |
| **Action** | Le verbe décrivant l'opération | `Read`, `Create`, `Update`, `Delete`, `Manage`, `Execute` |

### Actions standardisées

| Action | Signification | Quand l'utiliser |
| --- | --- | --- |
| `Read` | Consulter (liste + détail) | Accès en lecture seule |
| `Create` | Créer une nouvelle entité | Création uniquement |
| `Update` | Modifier une entité existante | Modification uniquement |
| `Delete` | Supprimer (soft ou hard delete) | Suppression uniquement |
| `Manage` | CRUD complet regroupé | Quand séparer les actions n'a pas de sens (ex : admin) |
| `Execute` | Déclencher une action | Import, export, job, action non-CRUD |

### Exemples

```text
# Permissions CRUD granulaires (module métier)
Invoices.Invoices.Read
Invoices.Invoices.Create
Invoices.Invoices.Update
Invoices.Invoices.Delete

# Permissions Granit framework
Authorization.Definitions.Read
Authorization.Grants.Manage
BackgroundJobs.Jobs.Manage
DataExchange.Imports.Execute
DataExchange.Exports.Execute
Timeline.Entries.Read
Timeline.Entries.Create
Workflow.History.Read
Localization.Overrides.Manage
```

### Règles

- **Pluriel** pour la ressource (`Imports`, pas `Import`)
- **Pas de `Write`** — utiliser `Create`, `Update` ou `Manage` selon le cas
- **`Manage`** regroupe tout le CRUD — réservé aux cas admin où séparer n'apporte rien
- **`Execute`** pour les actions non-CRUD (lancer un import, un export, un job)
- La constante C# doit correspondre : `DataExchangePermissions.Imports.Execute`
  renvoie `"DataExchange.Imports.Execute"`

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
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
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

## Endpoints REST — Granit.Authorization.Endpoints

Le package `Granit.Authorization.Endpoints` expose les permissions via des Minimal API,
permettant au frontend de vérifier les droits sans attendre un 403.

### Installation

```bash
dotnet add package Granit.Authorization.Endpoints
```

### Enregistrement

```csharp
// Module
[DependsOn(typeof(GranitAuthorizationEndpointsModule))]
public sealed class AppModule : GranitModule { }

// Program.cs
app.MapAuthorizationEndpoints(opts => opts.ApiPrefix = "api/v1");
```

### Routes

| Méthode | Route | Auth | Description |
| --- | --- | --- | --- |
| `GET` | `/auth/me` | Authentifié | Permissions de l'utilisateur courant |
| `GET` | `/auth/definitions` | `Authorization.Definitions.Read` | Toutes les définitions (groupées) |
| `GET` | `/auth/roles/{roleName}` | `Authorization.Grants.Manage` | Permissions accordées à un rôle |
| `PUT` | `/auth/roles/{roleName}/{permissionName}` | `Authorization.Grants.Manage` | Accorder une permission |
| `DELETE` | `/auth/roles/{roleName}/{permissionName}` | `Authorization.Grants.Manage` | Révoquer une permission |

### GET /auth/me — Réponse

```json
{
  "permissions": ["Invoices.Read", "Invoices.Create"]
}
```

L'endpoint itère toutes les permissions définies dans `IPermissionDefinitionManager` et
appelle `IPermissionChecker.IsGrantedAsync()` pour chaque. Seules les permissions
accordées sont retournées.

### GET /auth/definitions — Réponse

Les `displayName` sont résolus dans la langue de la requête HTTP (header
`Accept-Language` ou culture configurée). Si `IStringLocalizerFactory` n'est pas
enregistré, les clés de localisation sont retournées telles quelles.

```json
[
  {
    "name": "Invoices",
    "displayName": "Factures",
    "permissions": [
      { "name": "Invoices.Read", "displayName": "Consulter les factures" },
      { "name": "Invoices.Create", "displayName": "Créer des factures" }
    ]
  }
]
```

### Options

| Paramètre | Type | Défaut | Description |
| --- | --- | --- | --- |
| `ApiPrefix` | `string` | `""` | Préfixe API global (ex : `api/v1`) |
| `RoutePrefix` | `string` | `"auth"` | Préfixe de route pour les endpoints |
| `TagName` | `string` | `"Authorization"` | Tag OpenAPI |

### Permissions déclarées

Le module enregistre automatiquement deux permissions pour protéger les endpoints admin :

- `Authorization.Definitions.Read` — consulter les définitions
- `Authorization.Grants.Manage` — consulter, accorder et révoquer les grants

---

## Intégration frontend — usePermissions()

Le hook `usePermissions()` de `@granit/auth` appelle `GET /auth/me` et expose
les permissions sous forme de `Set<string>` pour un lookup O(1).

### Usage dans une application

```typescript
// src/features/auth/use-permissions.ts (wrapper app-level)
import { usePermissions as useGranitPermissions } from '@granit/auth';
import { api } from '@/lib/api';

export function usePermissions() {
  return useGranitPermissions({
    client: api,
    basePath: '/api/v1/auth',
  });
}
```

### Vérification dans un composant

```tsx
import { usePermissions, PermissionGuard } from '@/features/auth';

// Hook direct
const { hasPermission, isLoading } = usePermissions();
if (!hasPermission('Invoices.Delete')) return null;

// Composant garde (deny-by-default)
<PermissionGuard permission="Invoices.Delete">
  <DeleteButton />
</PermissionGuard>
```

### API du hook

| Propriété | Type | Description |
| --- | --- | --- |
| `permissions` | `ReadonlySet<string>` | Ensemble des permissions accordées |
| `hasPermission(name)` | `(string) => boolean` | Vérifie une permission |
| `hasAnyPermission(names)` | `(string[]) => boolean` | Au moins une accordée |
| `hasAllPermissions(names)` | `(string[]) => boolean` | Toutes accordées |
| `isLoading` | `boolean` | Chargement en cours |
| `error` | `Error \| null` | Erreur éventuelle |
| `refetch` | `() => void` | Force un rafraîchissement |

Les permissions sont mises en cache pour la durée de la session (`staleTime: Infinity`).
Appeler `refetch()` pour forcer un rafraîchissement après une modification de grants.

---

## Dépendances Granit

| Direction | Modules |
| --- | --- |
| **Dépend de** | `Granit.Core`, `Granit.Security`, `Granit.Caching` |
| **Utilisé par** | `Granit.Authorization.EntityFrameworkCore`, `Granit.Authorization.Endpoints`, `Granit.BackgroundJobs.Endpoints`, `Granit.Localization.Endpoints` |
| **Package EF Core** | `Granit.Authorization.EntityFrameworkCore` → ajoute `Granit.Persistence` |
| **Package Endpoints** | `Granit.Authorization.Endpoints` → ajoute `Granit.Authorization`, `Granit.Authorization.EntityFrameworkCore` |

> Voir le [graphe de dépendances complet](../dependencies.md).
