# Changelog

Tous les changements notables de ce projet seront documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

## [Unreleased]

### Added

- Initialisation du repository dd-foundation-dotnet
- Structure solution .NET 10 avec Central Package Management
- Projets : Abstractions, Security, Persistence, Vault, Observability
- Projets de tests associés
- CLAUDE.md, CONTRIBUTING.md, CI/CD pipeline
- `DigitalDynamics.Foundation.Caching` — abstraction `ICacheService<T>` + fournisseur Memory, protection stampede (double-check locking + SemaphoreSlim dans IMemoryCache), chiffrement AES-256-CBC opt-in par type via `[CacheEncrypted]`
- `DigitalDynamics.Foundation.Caching.StackExchangeRedis` — fournisseur Redis (StackExchange.Redis), activation `AesCacheValueEncryptor` automatique si `EncryptValues = true`
- `DigitalDynamics.Foundation.Caching.Hybrid` — fournisseur HybridCache L1+L2 pour Kubernetes multi-pods, `LocalCacheExpiration ≤ 60 s` pour borner la fenêtre de données obsolètes
- `DigitalDynamics.Foundation.Encryption` — `IStringEncryptionService` avec provider AES-256-CBC (`AesStringEncryptionProvider`) ; extensible via Vault Transit Engine
- `DigitalDynamics.Foundation.Localization` — localisation JSON modulaire par assembly (`EmbeddedResource`), héritage inter-modules (`[InheritResource]`), culture fallback natif, compatible `IStringLocalizer<T>`
- `DigitalDynamics.Foundation.Settings` — paramètres dynamiques avec résolution en cascade `User (U) → Tenant (T) → Global (G) → Configuration (C) → Default (D)`, cache intégré, `ISettingManager` / `ISettingProvider`, `InMemorySettingStore`

### Changed

- `DigitalDynamics.Foundation.Security` — module abstrait (interface `ICurrentUserService` uniquement) ; implémentation JWT Bearer déplacée dans `Foundation.Authentication.JwtBearer`
- `DigitalDynamics.Foundation.Authentication.JwtBearer` — nouveau package : JWT Bearer générique (section `"Authentication"`), `CurrentUserService`, policy `Authenticated`
- `DigitalDynamics.Foundation.Authentication.Keycloak` — nouveau package : extras Keycloak (`KeycloakClaimsTransformation`, `PostConfigure` JWT, policy `Admin`)
- `DigitalDynamics.Foundation.MultiTenancy` — découplé de `FoundationSecurityModule` (module autonome) ; `AuditedEntityInterceptor` injecte `TenantId` sur les entités `IMultiTenant`
- `DigitalDynamics.Foundation.Persistence` — hiérarchie de domaine : `Entity` → `CreationAuditedEntity` → `AuditedEntity` ; `ModelBuilderExtensions` applique les filtres multi-tenant et soft-delete
- `DigitalDynamics.Foundation.Vault` — `VaultClientFactory` utilise `IStringLocalizer<VaultLocalizationResource>` pour les messages d'erreur localisés (FR/EN)

---

## Procédure de mise à jour

### Quand mettre à jour ?

- **Toujours** lors d'une MR vers `main`
- **Jamais** lors d'un commit sur une branche feature

### Comment ?

1. Ajouter entrée dans `[Unreleased]` lors de la MR
2. Créer release lors du merge vers `main` (déplacer [Unreleased] vers nouvelle version)
3. Taguer la release : `git tag -a v0.1.0 -m "Release 0.1.0" && git push origin v0.1.0`

### Format des entrées

```markdown
### Added

- Courte description du changement (#issue ou !MR)

### Fixed

- Bug: description précise du bug corrigé (#issue)
```

### Catégories

- **Added** : Nouvelles fonctionnalités
- **Changed** : Changements dans les fonctionnalités existantes
- **Deprecated** : Fonctionnalités bientôt supprimées
- **Removed** : Fonctionnalités supprimées
- **Fixed** : Corrections de bugs
- **Security** : Corrections de vulnérabilités
