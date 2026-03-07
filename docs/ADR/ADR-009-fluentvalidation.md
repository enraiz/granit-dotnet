# ADR-009 : FluentValidation — Framework de validation métier

- **Statut** : Accepté
- **Date** : 2026-02-24
- **Issue** : [#183](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/183)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Validation, Granit.Wolverine)

## Contexte

La plateforme nécessite un framework de validation pour :

- **Validation métier** : règles complexes et composables (adresse, SIRET, IBAN,
  email, locale) avec codes d'erreur standardisés
- **Intégration Wolverine** : validation automatique des commandes avant exécution
  via le middleware pipeline (`WolverineFx.FluentValidation`)
- **Codes d'erreur** : mapping vers RFC 7807 ProblemDetails pour les réponses HTTP
- **Extensibilité** : validateurs custom réutilisables entre modules

## Décision

**FluentValidation** comme framework de validation métier.

## Alternatives évaluées

### Option 1 : FluentValidation (retenue)

- **Licence** : Apache-2.0
- **Avantage** : API fluent composable (`RuleFor(x => x.Email).EmailAddress()`),
  intégration Wolverine native, large communauté, validateurs custom faciles
- **Maturité** : 15+ ans, standard de facto pour la validation .NET

### Option 2 : DataAnnotations seules

- **Avantage** : natif .NET, zéro dépendance, intégré au model binding
- **Inconvénient** : limité aux validations simples (attributs), pas de
  composition, pas de validation conditionnelle complexe, pas d'intégration
  middleware Wolverine, codes d'erreur non standardisés

### Option 3 : MiniValidation

- **Licence** : MIT
- **Avantage** : léger, basé sur DataAnnotations avec extensions
- **Inconvénient** : pas de règles composables, pas d'intégration Wolverine,
  communauté limitée, ne couvre pas les cas métier complexes

### Option 4 : Validation custom (sans framework)

- **Avantage** : contrôle total, pas de dépendance
- **Inconvénient** : effort de développement et maintenance considérable,
  réinvention de la roue, pas de standards, pas de middleware pipeline

## Justification

| Critère | FluentValidation | DataAnnotations | MiniValidation | Custom |
| ------- | ---------------- | --------------- | -------------- | ------ |
| Licence | Apache-2.0 | Natif .NET | MIT | N/A |
| Règles composables | Oui | Non | Non | Manuel |
| Wolverine middleware | Natif | Non | Non | Manuel |
| Validation conditionnelle | Oui (When/Unless) | Non | Non | Manuel |
| Codes d'erreur | Oui (WithErrorCode) | Limité | Limité | Manuel |
| Communauté | Très large | Standard | Faible | N/A |
| RFC 7807 mapping | Via Granit.AspNetCore | Manuel | Manuel | Manuel |

## Conséquences

### Positives

- Validation déclarative et lisible dans chaque module
- Intégration Wolverine : les commandes sont validées avant exécution (DLQ si échec)
- Codes d'erreur standardisés Granit (ex. `VALIDATION.EMAIL.INVALID`)
- Validateurs réutilisables entre packages (`AddressValidator`, `SiretValidator`)
- Mapping automatique vers ProblemDetails RFC 7807 via `GranitExceptionHandler`

### Négatives

- Dépendance tierce pour la validation (risque de breaking changes majeures)
- Duplication partielle avec DataAnnotations pour les cas simples
  (conventions Granit : utiliser FluentValidation même pour les cas simples,
  pour la cohérence)
