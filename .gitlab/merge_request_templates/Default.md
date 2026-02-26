## Description

<!-- Décrivez clairement ce que cette MR apporte -->

### Contexte

<!-- Pourquoi ce changement est-il nécessaire ? -->

### Solution proposée

<!-- Comment avez-vous résolu le problème ? -->

---

## Références

**Issue liée** : Closes #XXX

---

## Type de changement

- [ ] Bug fix (changement non-breaking qui corrige un problème)
- [ ] Nouvelle fonctionnalité (changement non-breaking qui ajoute une fonctionnalité)
- [ ] Breaking change (fix ou feature qui casserait la compatibilité existante)
- [ ] Documentation uniquement
- [ ] Refactoring (pas de changement fonctionnel)
- [ ] Sécurité (patch de sécurité)
- [ ] Hotfix (fix urgent en production)

---

## Checklist

### Qualité du code

- [ ] Le code respecte les conventions du projet (IDE0008, IDE0022, ASP0025)
- [ ] `dotnet build` passe sans warnings
- [ ] `dotnet format --verify-no-changes` passe
- [ ] Pas de code commenté inutile
- [ ] Pas de TODO/FIXME (créer des issues séparées)

### Sécurité

- [ ] Aucun secret hardcodé (passwords, tokens, API keys)
- [ ] Pas de PII (données de santé) dans les logs
- [ ] Chiffrement conforme HDS (transit + repos)
- [ ] Souveraineté européenne respectée (OVHcloud FR)

### Tests

- [ ] Tests unitaires ajoutés/mis à jour (`dotnet test`)
- [ ] Couverture de code maintenue ou améliorée
- [ ] Pas de régression introduite

### Documentation

- [ ] `docs/` mis à jour (en français)
- [ ] Markdownlint passe sur les fichiers `.md` modifiés

### Compliance

- [ ] Changements respectent HDS/RGPD
- [ ] Données restent en Europe (OVHcloud FR)
- [ ] Rétrocompatibilité NuGet vérifiée (ou breaking change documenté dans CHANGELOG)

---

## Plan de test

**Tests effectués** :

1. ...
2. ...

**Rollback** : <!-- Comment revenir en arrière si problème ? -->

---

/label ~application
