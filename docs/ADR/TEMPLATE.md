# ADR-NNN : Titre court et descriptif

- **Statut** : Accepté | Proposé | Déprécié | Remplacé par [ADR-NNN](ADR-NNN-xxx.md)
- **Date** : YYYY-MM-DD
- **Issue** : [#NNN](URL) (si applicable)
- **Auteurs** : Jean-François Meyers
- **Portée** : repo-name (packages ou modules concernés)

> Voir aussi : [Titre ADR lié](URL) (si référence croisée inter-repo nécessaire)

## Contexte

Décrire le problème, le besoin fonctionnel ou technique, et les contraintes
(réglementaires, techniques, budgétaires) qui motivent la décision.

## Décision

**Résumé en gras de la décision prise.** Une à deux phrases.

## Alternatives évaluées

### Option 1 : Nom (retenue)

- **Licence** : MIT | Apache-2.0 | …
- **Avantage** : …
- (pas d'inconvénient listé pour l'option retenue — les limites sont dans Conséquences)

### Option 2 : Nom

- **Licence** : …
- **Avantage** : …
- **Inconvénient** : raison du rejet

### Option 3 : Nom

- …

## Justification

Tableau comparatif synthétique des alternatives sur les critères discriminants :

| Critère | Option 1 | Option 2 | Option 3 |
| ------- | -------- | -------- | -------- |
| Licence | MIT | Commercial | Apache-2.0 |
| Critère A | Oui | Non | Partiel |
| Critère B | … | … | … |

## Configuration déployée

> Section **optionnelle** — pertinente pour les décisions d'infrastructure ou
> les bibliothèques avec une configuration significative par environnement.

| Paramètre | Staging | Production |
| --------- | ------- | ---------- |
| Version | x.y.z | x.y.z |
| … | … | … |

## Conséquences

### Positives

- Point positif 1
- Point positif 2

### Négatives

- Compromis accepté 1
- Compromis accepté 2

## Actions de suivi

> Section **optionnelle** — liste les actions concrètes à réaliser après
> l'acceptation de l'ADR.

1. **Action 1** : description
2. **Action 2** : description

## Conditions de réévaluation

> Section **optionnelle** — décrit les signaux qui devraient déclencher une
> révision de cette décision.

Ce choix devrait être réévalué si :

- Condition 1
- Condition 2

## Références

> Section **optionnelle** — liens vers les commits, issues, modules,
> documentation externe et ADR liés.

- Commit initial : `abc1234` (YYYY-MM-DD, description)
- Issue : [#NNN — Titre](URL)
- Documentation : [lien](URL)
- ADR-NNN : [Titre](ADR-NNN-xxx.md)
