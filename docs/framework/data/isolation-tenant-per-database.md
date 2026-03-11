# Isolation Tenant-per-Database

## Vue d'ensemble

Le pattern **Tenant-per-Database** attribue une base de données physiquement
distincte à chaque tenant. C'est le niveau d'isolation le plus fort disponible dans Granit :
chaque tenant possède son propre schéma, ses propres index et ses propres credentials.

Ce pattern est recommandé pour les applications ISO 27001 traitant des données de santé sensibles,
ou pour les tenants dont le contrat exige une isolation physique (ex : CHU, établissements
soumis à des audits externes).

## Architecture

```mermaid
flowchart TD
    REQ["HTTP / Message"]
    CT["ICurrentTenant<br/>(tenantId)"]
    PROV["ITenantConnectionStringProvider"]
    FACTORY["TenantPerDatabaseDbContextFactory&lt;TContext&gt;"]
    DBA[("Base tenant A<br/>(PostgreSQL)")]
    DBB[("Base tenant B<br/>(PostgreSQL)")]
    DBC[("Base tenant C<br/>(PostgreSQL)")]

    REQ --> CT
    CT -->|tenantId| PROV
    PROV -->|connectionString| FACTORY
    FACTORY --> DBA
    FACTORY --> DBB
    FACTORY --> DBC
```

## Prérequis

- `Granit.Persistence` ≥ version courante
- `Granit.MultiTenancy` enregistré dans le module hôte
- Une base de données PostgreSQL provisionnée par tenant (Terraform, scripts de migration, etc.)
- Une implémentation de `ITenantConnectionStringProvider`

## Installation

> **Note** : les exemples utilisent `UseNpgsql()` (PostgreSQL). Granit est agnostique :
> tout provider EF Core est supporté (`UseSqlServer()`, `UseSqlite()`, etc.).

```csharp
// Program.cs
builder.Services.AddSingleton<ITenantConnectionStringProvider, VaultTenantConnectionStringProvider>();

builder.Services.AddTenantPerDatabaseDbContext<ApplicationDbContext>(
    static (opts, connectionString) => opts.UseNpgsql(connectionString));
```

L'extension enregistre :

- `IDbContextFactory<ApplicationDbContext>` → `TenantPerDatabaseDbContextFactory<ApplicationDbContext>`
- `ApplicationDbContext` (Scoped) → résolu via la factory

La sémantique `TryAdd` est utilisée : une factory déjà enregistrée (test d'intégration, override)
est conservée sans modification.

## Implémenter ITenantConnectionStringProvider

### Avec HashiCorp Vault (recommandé en production ISO 27001)

> `TenantDatabaseOptions`, `GetEncryptedCredential` et `BuildConnectionString` sont des
> exemples d'implémentation côté application — à adapter à votre configuration Vault et
> à votre catalogue de tenants.

```csharp
public sealed class VaultTenantConnectionStringProvider(
    ITransitEncryptionService vault,
    IOptions<TenantDatabaseOptions> options) : ITenantConnectionStringProvider
{
    public async Task<string> GetConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Résoudre les credentials dynamiques Vault pour ce tenant.
        string credential = await vault.DecryptAsync(
            options.Value.GetEncryptedCredential(tenantId), ct);

        return BuildConnectionString(tenantId, credential);
    }
}
```

### Avec une configuration statique (développement / tests)

```csharp
services.AddSingleton<ITenantConnectionStringProvider>(
    _ => new DelegateTenantConnectionStringProvider(tenantId =>
        Task.FromResult(configuration[$"Tenants:{tenantId}:ConnectionString"]
            ?? throw new InvalidOperationException($"Tenant {tenantId} not configured."))));
```

## Comportement en l'absence de tenant

Si `ICurrentTenant.IsAvailable` est `false` (aucun tenant résolu), la factory lève une
`InvalidOperationException` immédiatement, sans aucun fallback silencieux. Ce comportement
est intentionnel : accéder aux données sans contexte tenant violerait l'isolation RGPD/ISO 27001.

```text
InvalidOperationException: No active tenant context. Ensure the tenant is resolved before
accessing per-tenant data (HTTP: TenantResolutionMiddleware; messaging: TenantContextBehavior).
```

## Intégration Wolverine (Outbox + handler)

Quand `Granit.Wolverine.Postgresql` est utilisé, l'extension `AddGranitWolverineWithPostgresqlPerTenant<TContext>()`
configure automatiquement la factory avec Npgsql. Il n'est pas nécessaire d'appeler
`AddTenantPerDatabaseDbContext` en plus.

```csharp
// Program.cs — avec Wolverine Outbox
builder.Services.AddSingleton<ITenantConnectionStringProvider, VaultTenantConnectionStringProvider>();

builder.AddGranitWolverineWithPostgresqlPerTenant<ApplicationDbContext>();
```

## Audit trail ISO 27001

`AuditedEntityInterceptor` est câblé automatiquement dans chaque `DbContext` créé par la
factory lorsqu'il est présent dans le conteneur DI (enregistré par `AddGranitPersistence()`).
Cela garantit le suivi des opérations par tenant, conformément à l'exigence ISO 27001 de rétention
de 3 ans.

## Compatibilité par fournisseur de base de données

| Fournisseur         | Support | Notes                                                                |
| ------------------- | ------- | -------------------------------------------------------------------- |
| **PostgreSQL**      | Oui     | ISO 27001. Credentials dynamiques via Vault.                               |
| **SQL Server**      | Oui     | Un catalogue par tenant. Compatible Azure SQL.                       |
| **MySQL / MariaDB** | Oui     | Une base par tenant (synonyme de schema en MySQL).                   |
| **Oracle**          | Oui     | Un schema/user ou un PDB par tenant (multitenant Oracle).            |
| **SQLite**          | Oui     | Un fichier `.db` par tenant. Tests ou apps embarquees.               |
| **Cosmos DB**       | Oui     | Un container ou une database par tenant via connection string.       |

> `DatabasePerTenant` est la stratégie la plus universelle : tout provider EF Core la
> supporte, car il suffit de fournir une connection string différente par tenant.

## Considérations infrastructure

| Aspect | Recommandation |
| --- | --- |
| Provisionnement | Terraform + module PostgreSQL souverain |
| Credentials | HashiCorp Vault — credentials dynamiques, rotation automatique |
| Migrations | Une migration EF Core par tenant, déclenchée au déploiement |
| Connexions | PgBouncer recommandé si N tenants > 50 (réduction des connexions PostgreSQL) |
| Chiffrement | TLS obligatoire — `SslMode=Require` dans la connection string |

## Voir aussi

- [Compatibilité des fournisseurs EF Core](compatibilite-providers.md)
- [Isolation SharedDatabase](isolation-shared-database.md)
- [Isolation Tenant-per-Schema](isolation-tenant-per-schema.md)
- [Sélection de stratégie d'isolation](isolation-strategie.md)
- [Multi-tenancy — résolution du tenant](multi-tenancy.md)
