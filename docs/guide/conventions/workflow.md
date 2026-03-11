# Conventions Git et workflow

[← Conventions](index.md)

## Branching (GitFlow)

| Type de branche | Convention | Cible MR |
| --- | --- | --- |
| `feature/*` | Nouvelles fonctionnalités | `develop` |
| `fix/*` | Corrections de bugs | `develop` |
| `hotfix/*` | Correctifs urgents | `main` + `develop` |
| `release/*` | Stabilisation pré-release | `main` + `develop` |

**Push direct sur `main` interdit.**

## Cibles MR — règle stricte

| Type de branche | Cible par défaut | Exception |
| --- | --- | --- |
| `feature/*` | `develop` | Uniquement si demande explicite « target main » |
| `hotfix/*` | `main` + `develop` | Toujours les deux |
| `release/*` | `main` + `develop` | Toujours les deux |
| `fix/*` | `develop` | Uniquement si demande explicite « target main » |

Ne **jamais** cibler `main` pour une branche `feature/*` ou `fix/*` sauf demande
explicite. En cas de doute, demander avant de créer la MR.

## Commits (Conventional Commits, en français)

```text
feat(vault): ajouter le chiffrement Transit AES-256
fix(persistence): corriger l'intercepteur d'audit sur les entités détachées
docs(guide): créer le guide des conventions de codage
chore(ci): mettre à jour la pipeline GitLab CI
```

Types disponibles : `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `perf`.

## Dépendances tierces (THIRD-PARTY-NOTICES.md)

Chaque dépôt applicatif possède un fichier `THIRD-PARTY-NOTICES.md` à la racine qui
recense toutes les dépendances externes avec leur licence et copyright. Ce fichier
est une **obligation légale** pour les licences permissives (MIT, Apache-2.0, BSD, ISC).

**Lors de l'ajout, la suppression ou la montée de version d'une dépendance :**

1. **Mettre à jour** `THIRD-PARTY-NOTICES.md` — ajouter/modifier/supprimer l'entrée
   du package avec son nom, sa version, sa licence (identifiant SPDX) et le titulaire
   du copyright.
2. **Mettre à jour le tableau récapitulatif** en haut du fichier si le nombre de
   licences change.
3. **Mettre à jour la date** `Dernière mise à jour`.
4. **Signaler immédiatement** toute dépendance sous licence **non-permissive**
   (GPL, LGPL, AGPL, SSPL, ou restriction commerciale). Le contexte ISO 27001/commercial
   exige une revue juridique avant intégration.

**Ne jamais** ajouter ou mettre à jour une dépendance sans modifier
`THIRD-PARTY-NOTICES.md`.

## Releases

- Tags sémantiques sur `main` : `vMAJOR.MINOR.PATCH`
- Branches `release/*` pour la stabilisation avant le tag
- 1 approbation minimum pour merger vers `main`
