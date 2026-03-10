# Compatibilité des fournisseurs EF Core

Granit est un framework agnostique du fournisseur de base de données. Cependant, toutes
les fonctionnalités ne sont pas disponibles pour tous les providers EF Core. Ce document
centralise la matrice de compatibilité.

## Stratégies d'isolation multi-tenant

| Fournisseur         | SharedDatabase | SchemaPerTenant | DatabasePerTenant |
| ------------------- | -------------- | --------------- | ----------------- |
| **PostgreSQL**      | Oui            | Oui (défaut)    | Oui               |
| **SQL Server**      | Oui            | **Non**         | Oui               |
| **MySQL / MariaDB** | Oui            | Oui             | Oui               |
| **Oracle**          | Oui            | Oui             | Oui               |
| **SQLite**          | Oui            | **Non**         | Oui               |
| **Cosmos DB**       | Oui            | **Non**         | Oui               |

**Légende** :

- **SharedDatabase** : un filtre global `TenantId` sur chaque entité `IMultiTenant`.
  Universel, fonctionne avec tous les providers.
- **SchemaPerTenant** : bascule de schéma au niveau session
  (`SET search_path`, `USE`, `ALTER SESSION`).
  Requiert un provider supportant le switch de schéma par connexion.
- **DatabasePerTenant** : une connection string par tenant.
  Universel, fonctionne avec tous les providers.

> Pour choisir la bonne stratégie, voir
> [Sélection de stratégie d'isolation](isolation-strategie.md).

## SchemaPerTenant — détail par fournisseur

| Fournisseur         | Mécanisme                          | Activateur Granit                  |
| ------------------- | ---------------------------------- | ---------------------------------- |
| **PostgreSQL**      | `SET search_path TO "s", public`   | `PostgresqlTenantSchemaActivator`  |
| **MySQL / MariaDB** | `` USE `schema` ``                 | `MySqlTenantSchemaActivator`       |
| **Oracle**          | `ALTER SESSION SET CURRENT_SCHEMA` | `OracleTenantSchemaActivator`      |
| **SQL Server**      | —                                  | Non supporté (pas de SET SCHEMA)   |
| **SQLite**          | —                                  | Non supporté (pas de schémas)      |
| **Cosmos DB**       | —                                  | Non supporté (NoSQL, pas de DDL)   |

`PostgresqlTenantSchemaActivator` est enregistré par défaut via `TryAddSingleton`.
Pour un autre provider, enregistrer l'implémentation avant `AddTenantPerSchemaDbContext` :

```csharp
// MySQL / MariaDB
builder.Services.AddSingleton<ITenantSchemaActivator, MySqlTenantSchemaActivator>();

// Oracle
builder.Services.AddSingleton<ITenantSchemaActivator, OracleTenantSchemaActivator>();
```

Voir [ITenantSchemaActivator](isolation-tenant-per-schema.md) pour les détails.

## Fonctionnalités EF Core par provider

| Fonctionnalité                 | PgSQL | SQL Server | MySQL | Oracle | SQLite | Cosmos |
| ------------------------------ | ----- | ---------- | ----- | ------ | ------ | ------ |
| Collections de primitifs       | Oui   | Oui        | Oui   | Oui    | Oui    | Oui    |
| JSON natif (jsonb/json)        | Oui   | Oui        | Oui   | Non    | Non    | Natif  |
| Transactions distribuées       | Oui   | Oui        | Oui   | Oui    | Non    | Non    |
| Migrations EF Core             | Oui   | Oui        | Oui   | Oui    | Oui    | Non    |
| HasDefaultSchema               | Oui   | Oui        | Non   | Oui    | Non    | Non    |
| Wolverine transactional outbox | Oui   | Oui        | Non   | Non    | Non    | Non    |

### Notes par fournisseur

**PostgreSQL** :

- Support EF Core mature (Npgsql) : JSONB natif, arrays, full-text search
- Wolverine transactional outbox via `Granit.Wolverine.Postgresql`
- PgBouncer compatible avec `SET search_path` (mode transaction)

**SQL Server** :

- `SchemaPerTenant` non supporté : pas de `SET SCHEMA` session-level
- Alternative : `DatabasePerTenant` ou `SharedDatabase` avec filtre `TenantId`
- Wolverine transactional outbox via `WolverineFx.SqlServer`
- Attention Cloud Act pour données de santé (HDS)

**MySQL / MariaDB** :

- Schéma = database en MySQL (synonymes)
- `USE database` change la base active par connexion
- ProxySQL compatible

**Oracle** :

- `ALTER SESSION SET CURRENT_SCHEMA` change le schéma actif
- Un schéma Oracle = un utilisateur Oracle (grants nécessaires)
- Option Multitenant (PDB) pour isolation physique

**SQLite** :

- Pas de concept de schéma, pas de serveur
- `DatabasePerTenant` = un fichier `.db` par tenant
- Adapté aux tests d'intégration et apps embarquées

**Azure Cosmos DB** :

- NoSQL document store, pas de DDL classique
- Isolation par container (partition key `TenantId`) ou database par tenant
- Pas de migrations EF Core, pas de transactions multi-documents
- Attention Cloud Act pour données de santé (HDS)

## Recommandations par contexte

Le choix du provider est une décision applicative, pas du framework.
Granit fonctionne avec tous les providers EF Core listés ci-dessus.

| Contexte              | Stratégie recommandée |
| --------------------- | --------------------- |
| SaaS HDS (santé)      | DatabasePerTenant     |
| SaaS (> 100 tenants)  | SchemaPerTenant       |
| SaaS (> 1 000 tenants)| SharedDatabase        |
| Application interne   | SharedDatabase        |
| Tests d'intégration   | DatabasePerTenant     |
| Application mobile    | DatabasePerTenant     |

## Voir aussi

- [Sélection de stratégie d'isolation](isolation-strategie.md)
- [Isolation Tenant-per-Schema](isolation-tenant-per-schema.md)
- [Isolation Tenant-per-Database](isolation-tenant-per-database.md)
- [Multi-tenancy](multi-tenancy.md)
- [Persistence — intercepteurs EF Core](persistence.md)
