# Guide de contribution

> Comment contribuer aux packages Granit

---

## Code de conduite

- **Respect** : Traitez les autres avec courtoisie et professionnalisme
- **Qualité** : Privilégiez la qualité sur la rapidité
- **Sécurité** : La sécurité des données de santé est notre priorité absolue

---

## Workflow Git

### Prérequis

- **glab** : CLI GitLab (`glab auth login --hostname gitlab.digitaldynamics.be`)
- **.NET 10 SDK** : `dotnet --version` doit retourner `10.0.x`
- **Git** avec accès SSH au GitLab

### 1. Créer une branche

```text
<type>/<description-courte>

Types : feature/ | fix/ | hotfix/ | docs/ | refactor/ | chore/
```

### 2. Commits

**Conventional Commits** obligatoires :

```bash
git commit -m "feat(security): add Keycloak claims transformation"
git commit -m "fix(persistence): handle concurrent soft delete"
git commit -m "docs: update Vault integration guide"
```

### 3. Merge Request

1. Pousser votre branche
2. Créer une MR vers `main` (utiliser le template)
3. **1 approbation minimum** requise

---

## Standards de code

### C# / .NET

- **Target** : `net10.0`
- **Nullable** : activé (`<Nullable>enable</Nullable>`)
- **Warnings as errors** : activé
- **Central Package Management** : toutes les versions dans `Directory.Packages.props`
- **Namespaces** : `Granit.{Package}.{Sous-dossier}`

### Architecture des packages

- `Abstractions` ne dépend d'**aucun** autre package (ni Granit, ni tiers)
- Tous les autres packages référencent `Abstractions`
- Zéro référence circulaire entre packages
- Un projet = un package NuGet

### Tests

```bash
# Lancer tous les tests
dotnet test

# Tests d'un package spécifique
dotnet test tests/Granit.Security.Tests

# Avec couverture
dotnet test --collect:"XPlat Code Coverage"
```

- **Framework** : xUnit
- **Mocking** : NSubstitute
- **Assertions** : FluentAssertions
- **Données** : Bogus

### Sécurité

**JAMAIS** :

- Commiter des secrets ou credentials
- Logger des données de santé (PII) en clair
- Désactiver les scans de sécurité

**TOUJOURS** :

- Secrets via Vault / ExternalSecret
- Données de santé chiffrées en transit et au repos
- Audit trail pour toute opération sensible

---

## Processus de review

### Checklist du reviewer

- [ ] Aucun secret hardcodé
- [ ] Tests passent (`dotnet test`)
- [ ] Build réussi (`dotnet build`)
- [ ] Format vérifié (`dotnet format --verify-no-changes`)
- [ ] Pas de PII dans les logs
- [ ] CHANGELOG.md mis à jour
- [ ] Changements respectent HDS/RGPD

---

## Publication NuGet

Les packages sont publiés automatiquement sur GitLab Package Registry via CI/CD lors d'un tag `vX.Y.Z` sur `main`.

Pour publier manuellement (développement) :

```bash
dotnet pack -c Release -o ./nupkgs
dotnet nuget push ./nupkgs/*.nupkg --source gitlab
```

---

**Version** : 1.0
**Dernière mise à jour** : 2026-02-20
