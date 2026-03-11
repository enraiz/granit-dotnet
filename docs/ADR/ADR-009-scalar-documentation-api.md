# ADR-009 : Scalar.AspNetCore — Documentation API interactive

- **Statut** : Accepté
- **Date** : 2026-02-26
- **Issue** : [#80](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/80)
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.ApiDocumentation)

## Contexte

Les API REST de la plateforme nécessitent une interface de documentation
interactive pour les développeurs et intégrateurs. Depuis .NET 9, Microsoft
a retiré Swashbuckle (Swagger UI) du template par défaut au profit de
`Microsoft.AspNetCore.OpenApi` pour la génération de la spec OpenAPI.

Le choix de l'UI de documentation doit s'intégrer nativement avec le nouveau
pipeline OpenAPI .NET 10 sans dépendre de Swashbuckle.

## Décision

**Scalar.AspNetCore** comme UI de documentation OpenAPI interactive.

## Alternatives évaluées

### Option 1 : Scalar (retenue)

- **Licence** : MIT
- **Avantage** : intégration native `Microsoft.AspNetCore.OpenApi` (.NET 9+),
  UI moderne et responsive, « try it » intégré, thèmes personnalisables,
  recherche dans l'API, support OpenAPI 3.1
- **Configuration** : `app.MapScalarApiReference()` — une ligne

### Option 2 : Swagger UI (Swashbuckle)

- **Avantage** : standard historique, très large adoption
- **Inconvénient** : Swashbuckle est **abandonné** (dernière release 2023,
  retiré du template .NET 9 par Microsoft), dépendance à NSwag pour la
  génération de spec (doublon avec `Microsoft.AspNetCore.OpenApi`),
  UI datée

### Option 3 : ReDoc

- **Licence** : MIT
- **Avantage** : documentation statique élégante, trois colonnes
- **Inconvénient** : pas de « try it » natif (lecture seule), nécessite une
  intégration custom avec le pipeline OpenAPI .NET, moins interactif

### Option 4 : RapiDoc

- **Licence** : MIT
- **Avantage** : léger, web component, thèmes
- **Inconvénient** : communauté plus restreinte, pas d'intégration .NET officielle,
  maintenance irrégulière

### Option 5 : Stoplight Elements

- **Licence** : Apache-2.0
- **Avantage** : UI moderne, support API design
- **Inconvénient** : orienté SaaS (Stoplight Studio), intégration .NET non
  officielle, fonctionnalités avancées payantes

## Justification

| Critère | Scalar | Swagger UI | ReDoc | RapiDoc | Elements |
| ------- | ------ | ---------- | ----- | ------- | -------- |
| Licence | MIT | MIT | MIT | MIT | Apache-2.0 |
| Intégration .NET 10 | Native | Abandonné | Custom | Custom | Custom |
| Try-it interactif | Oui | Oui | Non | Oui | Oui |
| UI moderne | Oui | Non | Oui | Oui | Oui |
| Maintenance active | Oui | Non (2023) | Oui | Irrégulière | Oui |
| Configuration .NET | 1 ligne | ~10 lignes | Custom | Custom | Custom |
| OpenAPI 3.1 | Oui | Partiel | Oui | Oui | Oui |

## Conséquences

### Positives

- Intégration native avec `Microsoft.AspNetCore.OpenApi` (zéro Swashbuckle)
- UI moderne avec try-it, recherche, et thèmes
- Configuration minimale (`app.MapScalarApiReference()`)
- Compatible Asp.Versioning (versions multiples dans la spec)
- Maintenance active et releases régulières

### Négatives

- Scalar est moins connu que Swagger UI (courbe de familiarisation pour l'équipe)
- Projet relativement récent (2023) — moins de track record que Swagger UI
- Certaines fonctionnalités avancées (mocking, testing) sont dans Scalar Cloud (SaaS)
