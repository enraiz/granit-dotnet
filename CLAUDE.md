# CLAUDE.md - DigitalDynamics.Foundation

## Projet

- **Type** : Packages NuGet partagés pour les applications .NET Digital Dynamics
- **Repo** : `dd-foundation-dotnet` (niveau société, pas spécifique à un produit)
- **Cloud** : OVHcloud (Roubaix, FR) — souveraineté européenne
- **Compliance** : HDS + RGPD | Criticité ÉLEVÉE
- **Publication** : GitLab Package Registry (NuGet)

## Stack et versions

.NET 10 | C# 14 | EF Core 10 | VaultSharp 1.17+ | Serilog 9+ | OpenTelemetry 1.11+

## Packages

| Package | Rôle |
| --- | --- |
| `DigitalDynamics.Foundation.Core` | Système de modules (ABP-inspired), types domaine partagés |
| `DigitalDynamics.Foundation.Timing` | IClock, ICurrentTimezoneProvider, TimeProvider |
| `DigitalDynamics.Foundation.Guids` | IGuidGenerator, GUID séquentiels pour index clustered |
| `DigitalDynamics.Foundation.Security` | JWT Keycloak, ICurrentUserService, policies d'autorisation |
| `DigitalDynamics.Foundation.Persistence` | Intercepteurs EF Core (audit HDS, soft delete RGPD) |
| `DigitalDynamics.Foundation.Vault` | VaultSharp client, ITransitEncryptionService, credentials dynamiques |
| `DigitalDynamics.Foundation.Observability` | Serilog + OpenTelemetry → OTLP → Loki/Tempo/Mimir |

## Commandes

```bash
# Build
dotnet build

# Tests
dotnet test

# Pack NuGet (local)
dotnet pack -c Release -o ./nupkgs

# Format
dotnet format --verify-no-changes
```

## Contraintes réglementaires CRITIQUES

1. **Souveraineté** : Infrastructure DOIT rester en Europe (OVHcloud FR)
2. **US Cloud Act** : JAMAIS utiliser AWS/Azure/GCP pour données de santé
3. **HDS** : Audit trail 3 ans, chiffrement au repos et en transit
4. **RGPD** : Minimisation, droit à l'oubli, pseudonymisation
5. **Secrets** : Aucun secret en clair, rotation obligatoire

## Conventions de code

**C#** : PascalCase pour types et méthodes, camelCase pour paramètres et variables locales, `I` prefix pour interfaces, `Async` suffix pour méthodes async. Règles Roslyn strictes :

- **IDE0008** : TOUJOURS utiliser le type explicite au lieu de `var` (ex: `ServiceCollection services = new();` et non `var services = new ServiceCollection();`)
- **IDE0022** : Utiliser les expression body (`=>`) pour les méthodes à instruction unique
- **ASP0025** : Utiliser `AddAuthorizationBuilder()` au lieu de `AddAuthorization(Action<AuthorizationOptions>)` pour enregistrer les services d'autorisation

**Projets** : un projet = un package NuGet, namespace = nom du projet, zéro référence circulaire

**Core** : `DigitalDynamics.Foundation.Core` fournit le système de modules et les types domaine. Chaque module est auto-contenu (interface + implémentation dans le même package). Tous les packages Foundation référencent Core

**Tests** : chaque package a son projet de tests (`*.Tests`). xUnit + FluentAssertions + NSubstitute + Bogus. Les tests font partie de la DoD de chaque story

**Markdown** : Tous les fichiers `.md` doivent être conformes à markdownlint (config dans `.markdownlint.json`). Vérifier avec `npx markdownlint-cli2 "fichier.md"` avant de committer

**Diacritiques** : TOUJOURS utiliser les accents et diacritiques français corrects (é, è, ê, ë, à, â, ù, û, ô, î, ï, ç, œ) dans tous les contenus : fichiers Markdown, titres et descriptions d'issues GitLab, commentaires, commits

## Personas (rôles dans les user stories)

Le référentiel des personas est dans `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`.

**Personas disponibles** : SRE, Ingénieur DevOps, Développeur, Architecte, DBA, RSSI, DPO, CTO, Direction, Directeur juridique, Auditeur interne, Auditeur externe, Utilisateur, Professionnel de santé, Product Owner

**RÈGLES** : TOUJOURS utiliser un persona canonique. JAMAIS de rôles hybrides.

## GitLab issues

Avant toute opération GitLab, **invoquer le skill `/gitlab`** pour charger les commandes et conventions.

- **Types** : Epic (`[EPIC]`), Feature (`[FEATURE]`), Story (`[STORY]`) — pas d'emoji dans les titres
- **Hiérarchie** : GitLab Free, liens `relates_to` via API + références dans la description

## Git workflow

- **Branching** : Trunk-based (main + feature/* + hotfix/*)
- **Push direct sur `main` INTERDIT**
- **Releases** : Tags sémantiques sur main (vMAJOR.MINOR.PATCH)
- **Commits** : Conventional Commits (feat:, fix:, docs:, chore:)
- **MR** : 1 approbation minimum pour main

## Sécurité — Règles strictes

**TOUJOURS** : aucun secret hardcodé, `sensitive = true` pour les secrets, logs sans PII

**JAMAIS** : proposer solutions cloud US pour données de santé, stocker secrets en clair, désactiver l'audit logging

## Comportement attendu

- Comprendre le contexte HDS, RGPD, ISO 27001 et ISO 9001 avant de répondre
- Challenger les mauvaises pratiques de sécurité
- Fournir du code production-ready (pas de TODOs)
- Code : inclure header descriptif (description, inputs, outputs, usage)
