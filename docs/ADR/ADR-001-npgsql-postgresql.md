# ADR-001 : PostgreSQL via Npgsql comme SGBD de la plateforme

- **Statut** : Accepté
- **Date** : 2026-02-21
- **Issue** : [#8](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/8)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Persistence, Granit.Persistence.Migrations)

> Voir aussi : [IAC ADR-001 — Choix OVHcloud](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-001-choix-fournisseur-cloud-ovhcloud.md)
> (PostgreSQL HA déployé sur OVHcloud MKS — GRA9, Gravelines, France)

## Contexte

Le framework Granit fournit les abstractions de persistance (`Granit.Persistence`)
et l'orchestration des migrations (`Granit.Persistence.Migrations`). Le choix du
SGBD conditionne le provider EF Core, les extensions disponibles (JSONB, full-text
search), les stratégies de multi-tenancy et le coût d'exploitation.

Les contraintes réglementaires HDS imposent :

- **Traçabilité 3 ans** : audit trail sur toutes les opérations
- **Chiffrement au repos** : disques chiffrés (managed service OVHcloud)
- **Hébergement souverain** : données en Europe, hors Cloud Act US

## Décision

**PostgreSQL** comme SGBD de référence du framework Granit, via le provider
`Npgsql.EntityFrameworkCore.PostgreSQL`.

## Alternatives évaluées

### Option 1 : PostgreSQL via Npgsql (retenue)

- **Licence** : PostgreSQL License (permissive, OSI-approved)
- **Hébergement** : OVHcloud Managed PostgreSQL (GRA9, HDS-certifié)
- **Fonctionnalités** : JSONB, full-text search, partitioning, logical replication,
  extensions (pgcrypto, pg_stat_statements, PostGIS)
- **Multi-tenancy** : database-per-tenant et schema-per-tenant natifs
- **EF Core** : provider mature (`Npgsql.EntityFrameworkCore.PostgreSQL`)

### Option 2 : SQL Server

- **Licence** : commerciale (Developer gratuit, Production payant)
- **Avantage** : intégration Microsoft native, outillage SSMS
- **Inconvénient** : coût de licence en production, pas de managed service
  OVHcloud (nécessiterait Azure SQL — **incompatible Cloud Act / HDS**),
  vendor lock-in Microsoft

### Option 3 : MySQL / MariaDB

- **Licence** : GPL (MySQL), GPL/BSL (MariaDB)
- **Avantage** : large adoption, managed OVHcloud disponible
- **Inconvénient** : pas de JSONB natif performant, provider EF Core moins mature
  (Pomelo), limitations du partitioning et des schémas, pas de support natif
  database-per-schema pour le multi-tenancy

### Option 4 : CockroachDB

- **Licence** : BSL 1.1 (usage limité sans licence commerciale)
- **Avantage** : distribution géographique native, compatibilité PostgreSQL wire protocol
- **Inconvénient** : licence restrictive, surcoût opérationnel pour un MVP,
  pas de managed service OVHcloud, communauté .NET limitée

## Justification

| Critère | PostgreSQL | SQL Server | MySQL/MariaDB | CockroachDB |
| ------- | ---------- | ---------- | ------------- | ----------- |
| Licence OSS | PostgreSQL (libre) | Commercial | GPL | BSL |
| Managed OVHcloud | Oui (HA) | Non | Oui | Non |
| Souveraineté HDS | Oui | Non (Azure) | Oui | Non |
| JSONB natif | Oui | Partiel | Non | Partiel |
| Provider EF Core | Npgsql (mature) | Natif MS | Pomelo (tiers) | Npgsql compat |
| Multi-tenancy avancé | DB + Schema | DB uniquement | DB uniquement | DB |
| Coût | Inclus OVHcloud | Licence + infra | Inclus OVHcloud | Licence + infra |

## Conséquences

### Positives

- SGBD open source sans coût de licence
- Managed service OVHcloud avec HA et backups automatiques (HDS)
- Écosystème riche d'extensions (pgcrypto pour le chiffrement, pg_stat_statements
  pour le monitoring)
- Support natif des stratégies multi-tenancy avancées (Granit.Persistence)
- JSONB pour les structures semi-structurées (settings, audit metadata)

### Négatives

- L'équipe doit maîtriser les spécificités PostgreSQL (VACUUM, bloat, pg_stat)
- Certaines fonctionnalités EF Core sont PostgreSQL-spécifiques (non portable)
- Outillage DBA moins riche que SQL Server (pas de SSMS, alternatives : pgAdmin,
  DBeaver)

## Conditions de réévaluation

Ce choix devrait être réévalué si :

- OVHcloud déprécie le service Managed PostgreSQL
- Une exigence réglementaire impose un SGBD spécifique (ex. certification ANSSI)
- Le volume de données ou les patterns d'accès nécessitent un SGBD NoSQL ou distribué

## Références

- Commit initial : `52f1444` (2026-02-21)
- Issue : [#8 — Granit.Persistence](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/8)
- IAC ADR-001 : [Choix OVHcloud](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-001-choix-fournisseur-cloud-ovhcloud.md)
- IAC ADR-003 : [Vault sur Kubernetes](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-003-vault-sur-kubernetes.md) (credentials dynamiques PostgreSQL)
- Npgsql : <https://www.npgsql.org/>
