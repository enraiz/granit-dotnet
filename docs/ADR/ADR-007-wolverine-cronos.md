# ADR-007 : Wolverine + Cronos — Messaging, CQRS et scheduling

- **Statut** : Accepté
- **Date** : 2026-02-22
- **Issue** : [#115](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/115)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Wolverine, Granit.Wolverine.Postgresql, Granit.BackgroundJobs)

## Contexte

La plateforme nécessite :

- **Messaging asynchrone** : envoi de commandes et d'événements entre modules
  (domain events, integration events) avec garantie de livraison
- **Outbox transactionnel** : les messages doivent être persistés dans la même
  transaction que les changements métier (consistance éventuelle sans perte)
- **CQRS** : séparation commande/query avec un médiateur intégré
- **Background jobs** : exécution de tâches récurrentes (synchronisation, nettoyage,
  rapports) avec scheduling cron et résilience multi-instance
- **Pas de broker externe** : pour le MVP, éviter la complexité opérationnelle
  d'un RabbitMQ ou Kafka — PostgreSQL doit suffire comme transport

Cronos est utilisé comme parser d'expressions cron dans le module
`Granit.BackgroundJobs` pour la planification des jobs récurrents.

## Décision

- **Wolverine** (WolverineFx) comme bus de messages, médiateur et framework
  de handlers avec outbox PostgreSQL
- **Cronos** comme parser d'expressions cron pour le scheduling des background jobs

## Alternatives évaluées

### Messaging / Médiateur

#### Wolverine (retenu)

- **Licence** : MIT (JasperFx)
- **Outbox** : transactionnel EF Core natif (`WolverineFx.EntityFrameworkCore`)
- **Transport** : PostgreSQL natif (`WolverineFx.Postgresql`) — pas de broker requis
- **Pipeline** : middleware composable (validation, retry, DLQ, logging)
- **Handlers** : convention-based (pas d'interface à implémenter), découverte auto
- **Intégration** : FluentValidation middleware natif, support multi-tenancy

#### MassTransit

- **Licence** : Apache-2.0
- **Avantage** : très mature, large communauté, support multi-transport
  (RabbitMQ, Azure SB, Amazon SQS, in-memory)
- **Inconvénient** : nécessite un broker externe pour la production (RabbitMQ
  minimum), configuration plus verbeuse, outbox EF Core disponible mais
  moins intégré que Wolverine, pas de support PostgreSQL-as-transport natif

#### MediatR

- **Licence** : Apache-2.0
- **Avantage** : simple, léger, pattern médiateur pur
- **Inconvénient** : pas d'outbox, pas de transport, pas de retry/DLQ,
  pas de scheduling — uniquement un médiateur in-process. Nécessite de
  combiner avec un autre outil pour le messaging asynchrone

#### Brighter

- **Licence** : MIT
- **Avantage** : support outbox, pipeline de middlewares
- **Inconvénient** : communauté plus restreinte, documentation moins fournie,
  configuration plus complexe que Wolverine

#### NServiceBus

- **Licence** : commerciale (Particular Software)
- **Avantage** : solution entreprise complète, saga support, monitoring
- **Inconvénient** : licence payante, incompatible avec la stratégie OSS du projet

### Scheduling / Cron

#### Cronos (retenu)

- **Licence** : MIT
- **Avantage** : parser cron léger et rapide, support des secondes optionnel,
  calcul du prochain déclenchement sans état
- **Utilisation** : intégré dans `RecurringJobAttribute` et `CronSchedulerAgent`

#### Quartz.NET

- **Avantage** : scheduler complet avec persistance, clustering, triggers avancés
- **Inconvénient** : surdimensionné (scheduler complet alors que Wolverine gère
  déjà l'exécution), duplication de responsabilité, configuration lourde

#### Hangfire

- **Licence** : LGPL-3.0 (core), commercial (Pro)
- **Avantage** : dashboard intégré, jobs récurrents, retry automatique
- **Inconvénient** : doublon avec Wolverine (transport, retry, DLQ), licence
  restrictive pour les fonctionnalités avancées

#### NCrontab

- **Avantage** : parser cron simple et léger
- **Inconvénient** : pas de support des secondes, API moins moderne que Cronos,
  maintenance réduite

## Justification

### Messaging

| Critère | Wolverine | MassTransit | MediatR | Brighter | NServiceBus |
| ------- | --------- | ----------- | ------- | -------- | ----------- |
| Licence | MIT | Apache-2.0 | Apache-2.0 | MIT | Commercial |
| Outbox EF Core | Natif | Oui | Non | Oui | Oui |
| PostgreSQL transport | Natif | Non | N/A | Non | Non |
| Broker requis | Non | Oui (prod) | N/A | Oui | Oui |
| Pipeline middleware | Oui | Oui | Oui | Oui | Oui |
| FluentValidation | Natif | Tiers | Tiers | Non | Non |
| Convention-based | Oui | Partiel | Non | Non | Non |
| Multi-tenancy | Oui | Oui | Non | Non | Oui |

### Scheduling

| Critère | Cronos | Quartz.NET | Hangfire | NCrontab |
| ------- | ------ | ---------- | -------- | -------- |
| Licence | MIT | Apache-2.0 | LGPL/Commercial | Apache-2.0 |
| Scope | Parser seul | Scheduler complet | Scheduler complet | Parser seul |
| Secondes | Optionnel | Oui | Non | Non |
| Poids | Très léger | Lourd | Moyen | Léger |

## Conséquences

### Positives

- Pas de broker externe : PostgreSQL suffit comme transport (simplicité opérationnelle)
- Outbox transactionnel : zéro perte de message, consistance éventuelle garantie
- Pipeline Wolverine unifié : validation, retry, DLQ, logging, tracing
- Cronos léger : juste un parser, l'orchestration est gérée par Wolverine
- Licence MIT pour l'ensemble de la stack

### Négatives

- Wolverine est moins connu que MassTransit (communauté plus restreinte)
- Dépendance sur JasperFx (mainteneur principal : Jeremy D. Miller)
- Si le besoin d'un broker apparaît (RabbitMQ, Kafka), migration nécessaire
  (Wolverine supporte RabbitMQ et Azure SB, mais pas Kafka nativement)
- PostgreSQL-as-transport a des limites de débit vs un broker dédié
