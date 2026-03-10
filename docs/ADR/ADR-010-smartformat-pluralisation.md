# ADR-011 : SmartFormat.NET — Pluralisation CLDR

- **Statut** : Accepté
- **Date** : 2026-02-26
- **Issue** : [#23](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/23)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Localization)

## Contexte

Le module `Granit.Localization` fournit un système de localisation JSON modulaire.
La pluralisation est une fonctionnalité critique pour les applications multilingues :
les règles CLDR (Unicode Common Locale Data Repository) définissent des catégories
de pluralisation qui varient selon les langues (français : singulier/pluriel,
arabe : 6 formes, etc.).

## Décision

**SmartFormat.NET** pour la pluralisation dans le système de localisation.

## Alternatives évaluées

### Option 1 : SmartFormat.NET (retenue)

- **Licence** : MIT
- **Avantage** : support CLDR complet (toutes les langues), syntaxe familière
  (`{0:plural:…}`), léger (~50 Ko), pas de dépendance native (ICU), extensible
  via custom formatters
- **Maturité** : 12+ ans, actif

### Option 2 : ICU4N

- **Avantage** : implémentation officielle ICU pour .NET, support MessageFormat
- **Inconvénient** : dépendance native ICU (problèmes de déploiement cross-platform,
  taille ~30 Mo), API complexe, documentation .NET limitée

### Option 3 : MessageFormat.NET

- **Avantage** : implémentation du standard ICU MessageFormat
- **Inconvénient** : projet peu maintenu, communauté restreinte, documentation
  insuffisante, pas de support CLDR complet

### Option 4 : Pluralisation custom

- **Avantage** : contrôle total, zéro dépendance
- **Inconvénient** : réimplémenter les règles CLDR (200+ langues) est un effort
  considérable et source d'erreurs, maintenance à long terme

### Option 5 : gettext (.po files)

- **Avantage** : standard de l'industrie pour la localisation, outillage mature
- **Inconvénient** : format fichier .po incompatible avec le système JSON de Granit,
  nécessite une refonte complète du pipeline de localisation, écosystème .NET limité

## Justification

| Critère | SmartFormat | ICU4N | MessageFormat | Custom | gettext |
| ------- | ----------- | ----- | ------------- | ------ | ------- |
| Licence | MIT | MIT | MIT | N/A | GPL/LGPL |
| CLDR complet | Oui | Oui | Partiel | Non | Oui |
| Dépendance native | Non | Oui (ICU) | Non | Non | Non |
| Taille | ~50 Ko | ~30 Mo | ~20 Ko | 0 | Variable |
| Documentation .NET | Bonne | Faible | Faible | N/A | Faible |
| Intégration JSON | Facile | Possible | Possible | N/A | Incompatible |

## Conséquences

### Positives

- Pluralisation correcte pour toutes les langues sans dépendance native
- Syntaxe concise et lisible dans les fichiers de traduction JSON
- Léger : pas d'impact significatif sur la taille du package
- Extensible : custom formatters pour les cas métier spécifiques

### Négatives

- SmartFormat a sa propre syntaxe (pas un standard ICU MessageFormat pur)
- Si un passage à ICU MessageFormat est nécessaire, migration des fichiers
  de traduction requise
