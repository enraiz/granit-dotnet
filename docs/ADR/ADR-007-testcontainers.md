# ADR-007 : Testcontainers — Tests d'intégration conteneurisés

- **Statut** : Accepté
- **Date** : 2026-02-24
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.Wolverine.Postgresql.IntegrationTests)

## Contexte

Les tests d'intégration de Granit nécessitent une base de données PostgreSQL
réelle pour valider les comportements spécifiques au SGBD : migrations EF Core,
outbox Wolverine, filtres globaux multi-tenant, requêtes JSONB, etc.

Les alternatives in-memory (EF Core InMemory, SQLite) ne reproduisent pas
fidèlement le comportement PostgreSQL et masquent des bugs qui n'apparaissent
qu'en production.

## Décision

**Testcontainers** (`Testcontainers.PostgreSql`) pour orchestrer des conteneurs
PostgreSQL éphémères dans les tests d'intégration.

## Alternatives évaluées

### Option 1 : Testcontainers (retenue)

- **Licence** : MIT
- **Avantage** : conteneur PostgreSQL réel démarré à la demande, isolation
  complète par test, nettoyage automatique, API fluent .NET, support xUnit
  via `IAsyncLifetime`
- **CI** : compatible GitLab CI (Docker-in-Docker ou service container)

### Option 2 : EF Core InMemory

- **Avantage** : rapide, zéro dépendance infrastructure
- **Inconvénient** : pas de SQL réel (pas de migrations, pas de contraintes FK,
  pas de JSONB, pas de transactions), faux sentiment de confiance,
  bugs masqués en production

### Option 3 : SQLite (EF Core)

- **Avantage** : SQL réel sans serveur, rapide
- **Inconvénient** : dialecte SQL différent de PostgreSQL (pas de JSONB,
  pas de schémas, types différents), migrations non portables,
  comportement transactionnel différent

### Option 4 : Base de test PostgreSQL partagée

- **Avantage** : pas de Docker, rapidité (pas de démarrage conteneur)
- **Inconvénient** : state partagé entre tests (isolation difficile),
  nettoyage manuel, CI non reproductible (dépend d'un serveur externe),
  conflits entre développeurs

## Justification

| Critère | Testcontainers | InMemory | SQLite | Base partagée |
| ------- | -------------- | -------- | ------ | ------------- |
| Fidélité PostgreSQL | Totale | Nulle | Partielle | Totale |
| Isolation | Par test | Par test | Par test | Difficile |
| Reproductibilité CI | Oui | Oui | Oui | Non |
| Vitesse | Moyen (~3-5s init) | Très rapide | Rapide | Rapide |
| Zéro infra externe | Oui (Docker) | Oui | Oui | Non |
| Migrations EF Core | Oui | Non | Partiel | Oui |

## Conséquences

### Positives

- Tests fidèles au comportement production (vrai PostgreSQL)
- Isolation complète : chaque suite de tests a sa propre base
- CI reproductible sans dépendance externe
- Détection précoce des bugs liés au SGBD (types, contraintes, transactions)

### Négatives

- Nécessite Docker sur les postes de développement et en CI
- Temps de démarrage du conteneur (~3-5 secondes par suite de tests)
- Consommation mémoire plus élevée que les alternatives in-memory
