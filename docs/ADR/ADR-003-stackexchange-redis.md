# ADR-003 : Redis via StackExchange.Redis — Cache distribué

- **Statut** : Accepté
- **Date** : 2026-02-21
- **Issue** : [#25](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/25)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Caching, Granit.Caching.StackExchangeRedis, Granit.Caching.Hybrid)

## Contexte

Le framework Granit fournit les abstractions de cache (`Granit.Caching`) et
nécessite un backend de cache distribué pour :

- **Performance** : réduire la latence des lectures fréquentes (settings, traductions,
  templates, permissions)
- **Scalabilité** : cache partagé entre les instances Kubernetes (sticky sessions
  impossibles en contexte HDS — haute disponibilité requise)
- **Idempotence** : stockage des clés d'idempotence HTTP
- **SignalR** : backplane Redis pour les notifications temps réel

Le choix du backend de cache conditionne l'implémentation de
`Granit.Caching.StackExchangeRedis` et le pattern L1+L2 de `Granit.Caching.Hybrid`.

## Décision

**Redis** via **StackExchange.Redis** comme backend de cache distribué (L2),
combiné avec `Microsoft.Extensions.Caching.Hybrid` pour le pattern L1+L2.

## Alternatives évaluées

### Option 1 : Redis via StackExchange.Redis (retenue)

- **Licence** : MIT (StackExchange.Redis)
- **Avantage** : standard de facto, intégration native `IDistributedCache`,
  support HybridCache, Pub/Sub pour invalidation, backplane SignalR

### Option 2 : Memcached

- **Avantage** : simple, léger, performant pour du key-value pur
- **Inconvénient** : pas de Pub/Sub, pas de structures de données avancées,
  pas de persistance, pas de backplane SignalR

### Option 3 : NCache

- **Avantage** : solution .NET native, topologies avancées
- **Inconvénient** : licence commerciale, communauté restreinte,
  pas d'intégration HybridCache standard

### Option 4 : Microsoft Garnet

- **Avantage** : compatible Redis protocol, performances supérieures
- **Inconvénient** : projet récent (2024), pas de managed service, risque
  de stabilité pour un usage production HDS

## Justification

| Critère | SE.Redis | Memcached | NCache | Garnet |
| ------- | -------- | --------- | ------ | ------ |
| Licence client | MIT | Apache-2.0 | Freemium | MIT |
| IDistributedCache | Natif MS | Tiers | Tiers | Compatible |
| HybridCache .NET 10 | Oui | Non | Non | Compatible |
| Pub/Sub | Oui | Non | Oui | Oui |
| SignalR backplane | Oui (MS officiel) | Non | Non | Non testé |
| Maturité | 10+ ans | Mature | Mature | Récent |

## Conséquences

### Positives

- Intégration native avec le DI Microsoft (`IDistributedCache`, `HybridCache`)
- Client MIT, stable et très largement adopté
- Pub/Sub pour l'invalidation de cache et backplane SignalR
- Pipeline HybridCache L1+L2 transparent via `Granit.Caching`

### Négatives

- Redis est une dépendance infrastructure supplémentaire à opérer
- Licence Redis 7.4+ (SSPL) : à surveiller si self-hosted
- Sérialisation des objets complexes nécessite une stratégie cohérente

## Conditions de réévaluation

Ce choix devrait être réévalué si :

- Microsoft Garnet atteint la maturité production et offre un managed service
- La licence Redis (SSPL) devient problématique pour le déploiement self-hosted
- Les besoins de cache évoluent vers un pattern incompatible avec Redis
  (ex. cache distribué géographiquement)

## Références

- Commit initial : `76378865` (2026-02-21)
- Issues : [#25](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/25), [#27](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/27), [#28](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/28)
- StackExchange.Redis : <https://github.com/StackExchange/StackExchange.Redis>
