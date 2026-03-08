# Référence API C\#

Cette section contient la documentation de référence de l'API publique Granit,
générée automatiquement à partir des commentaires XML (`/// <summary>`) du code source.

## Génération

La référence est produite par [DocFX](https://dotnet.github.io/docfx/) à chaque
exécution du pipeline CI/CD. Elle reflète toujours l'état du code sur la branche
`develop` ou le dernier tag de release.

## Contenu

Chaque package Granit expose une API publique documentée :

- **Classes et interfaces** : description, paramètres de constructeur, propriétés
- **Méthodes d'extension** : signature complète avec types explicites
- **Options de configuration** : propriétés, valeurs par défaut, attributs de validation
- **Énumérations et records** : membres et documentation associée

## Navigation

Utilisez la table des matières à gauche pour naviguer par namespace.
Les namespaces suivent la convention `Granit.<Package>` (exemple : `Granit.Caching`,
`Granit.Security`, `Granit.Persistence`).

## API externes

Voir [API externes](external-apis.md) pour la documentation complète des services
tiers appelés par Granit (Brevo, Keycloak, SMTP, Web Push, Vault, S3, Webhooks, OTLP) :
authentification, endpoints, configuration, politiques de résilience.

## Compléments

La référence API est un complément aux guides thématiques :

| Section | Description |
| --- | --- |
| [Framework](../framework/index.md) | Architecture, modules, sécurité, données, diagnostics |
| [Guide](../guide/index.md) | Tutoriels pas-à-pas, démarrage rapide |
| [Cookbook](../cookbook/index.md) | Recettes pratiques cross-modules |
| [Tests](../testing/index.md) | Conventions, mocking, assertions, intégration |
