# Déploiement et exploitation

Cette section couvre les aspects opérationnels de la mise en production
d'une application Granit sur infrastructure souveraine OVHcloud.

## Audience

- **SRE** : configuration observabilité, alerting, incident response
- **Ingénieur DevOps** : pipelines CI/CD, Kubernetes, Helm
- **Architecte** : choix d'infrastructure, dimensionnement, conformité

## Guides

| Guide | Description |
| --- | --- |
| [Pipeline CI/CD](ci-cd-pipeline.md) | Stages, cache NuGet, tests d'intégration DinD, SonarQube, packaging |
| [Observabilité en production](observabilite-production.md) | Stack LGTM, dashboards Grafana, queries LogQL, alerting |
| [Configuration Vault](configuration-vault.md) | Authentification K8s, credentials dynamiques, Transit, rotation |
| [Déploiement Kubernetes](kubernetes.md) | Probes santé, resource limits, rolling update, graceful shutdown |
| [Checklist mise en production](checklist-mise-en-production.md) | Vérification RGPD/ISO 27001, sécurité, performance |

## Infrastructure souveraine

Toute application Granit traitant des données de santé **doit** être hébergée
sur infrastructure européenne conforme ISO 27001 :

- **Compute** : OVHcloud Managed Kubernetes (Roubaix, FR)
- **Base de données** : PostgreSQL (OVHcloud ou autogéré)
- **Cache** : Redis (OVHcloud ou autogéré)
- **Secrets** : HashiCorp Vault (autogéré, stockage Raft)
- **Observabilité** : stack LGTM autogérée (Loki, Grafana, Tempo, Mimir)
- **Object Storage** : OVHcloud S3 (compatible AWS SDK)

> **US Cloud Act** : AWS, Azure et GCP sont **interdits** pour les données
> de santé soumises au référentiel ISO 27001.
