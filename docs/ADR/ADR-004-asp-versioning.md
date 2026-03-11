# ADR-004 : Asp.Versioning — Versionnement d'API REST

- **Statut** : Accepté
- **Date** : 2026-02-22
- **Issue** : [#78](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/78)
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.ApiVersioning)

## Contexte

Les API REST de la plateforme doivent supporter le versionnement pour
permettre l'évolution des contrats sans casser les clients existants. Ce besoin
est particulièrement critique dans un contexte santé (HDS) où les intégrateurs
tiers (laboratoires, DPI) ont des cycles de mise à jour longs.

Le versionnement doit être :

- **Explicite** : chaque endpoint déclare sa version
- **Négociable** : le client choisit la version via URL, header ou query string
- **Documenté** : les versions apparaissent dans l'OpenAPI spec (Scalar UI)

## Décision

**Asp.Versioning.Mvc** (+ ApiExplorer) pour le versionnement sémantique des API.

## Alternatives évaluées

### Option 1 : Asp.Versioning (retenue)

- **Licence** : MIT (.NET Foundation)
- **Avantage** : package officiel .NET Foundation (ex-Microsoft.AspNetCore.Mvc.Versioning),
  support URL segment (`/api/v1/…`), header (`api-version`), query string (`?api-version=1`),
  media type. Intégration ApiExplorer pour OpenAPI
- **Maturité** : 8+ ans, migration depuis le package Microsoft historique

### Option 2 : Versionnement URL manuel (convention de routage)

- **Avantage** : zéro dépendance, simple pour des cas basiques
- **Inconvénient** : pas de négociation de version, pas de sunset policies,
  duplication de code entre versions, pas d'intégration OpenAPI automatique

### Option 3 : Convention de nommage custom (namespace-based)

- **Avantage** : organisation claire du code par namespace/version
- **Inconvénient** : nécessite un framework maison, pas de standard,
  maintenance et documentation à charge de l'équipe

## Justification

| Critère | Asp.Versioning | URL manuel | Custom |
| ------- | -------------- | ---------- | ------ |
| Standard .NET | Oui (.NET Foundation) | Non | Non |
| Modes de version | URL, header, QS, media type | URL seul | Variable |
| OpenAPI intégré | Oui (ApiExplorer) | Non | Non |
| Sunset policies | Oui | Non | Non |
| Effort maintenance | Nul (communauté) | Élevé | Très élevé |

## Conséquences

### Positives

- Standard de l'écosystème .NET, documentation abondante
- Versionnement multi-modal (URL segment par défaut dans Granit)
- Intégration automatique avec Scalar UI via ApiExplorer
- Sunset headers pour la dépréciation progressive des anciennes versions

### Négatives

- Version preview (10.0.0-preview.1) pour .NET 10 — à surveiller
- Configuration initiale nécessaire (convention par défaut dans `GranitApiVersioningModule`)
