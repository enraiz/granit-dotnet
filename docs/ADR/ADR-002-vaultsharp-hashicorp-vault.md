# ADR-002 : HashiCorp Vault via VaultSharp — Gestion des secrets

- **Statut** : Accepté
- **Date** : 2026-02-21
- **Issue** : [#9](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/9)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Vault)

> Voir aussi : [IAC ADR-003 — Vault sur Kubernetes](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-003-vault-sur-kubernetes.md)
> (déploiement Vault HA Raft sur OVHcloud MKS)

## Contexte

La certification HDS impose des exigences strictes sur la gestion des secrets :

- **Rotation obligatoire** des credentials (base de données, API keys)
- **Chiffrement en transit et au repos** des données sensibles
- **Audit trail** de chaque accès aux secrets (qui, quand, quel secret)
- **Pas de secrets en clair** dans le code, les fichiers de configuration ou les
  variables d'environnement non chiffrées

Le module `Granit.Vault` encapsule l'intégration avec un gestionnaire de secrets
et expose les abstractions de chiffrement Transit. Le choix du fournisseur de
secrets management conditionne les capacités de rotation, d'audit et de
chiffrement applicatif.

## Décision

**HashiCorp Vault** comme gestionnaire de secrets, avec **VaultSharp**
comme client .NET (via `Granit.Vault`).

## Alternatives évaluées

### Option 1 : HashiCorp Vault + VaultSharp (retenue)

- **Licence** : BSL 1.1 (Vault), Apache-2.0 (VaultSharp)
- **Déploiement** : self-hosted sur OVHcloud (souveraineté totale)
- **Fonctionnalités** : KV secrets, Transit encryption, dynamic database credentials,
  auto-unseal, audit backend, policies granulaires

### Option 2 : Azure Key Vault

- **Avantage** : intégration native .NET, SDK Microsoft officiel
- **Inconvénient** : **incompatible souveraineté** — hébergé sur Azure (Cloud Act US),
  pas de transit encryption côté serveur, pas de credentials dynamiques natifs

### Option 3 : AWS Secrets Manager

- **Avantage** : rotation automatique, intégration AWS
- **Inconvénient** : **incompatible souveraineté** — hébergé sur AWS (Cloud Act US),
  pas de transit encryption

### Option 4 : CyberArk

- **Avantage** : solution entreprise certifiée, audit avancé
- **Inconvénient** : coût de licence très élevé, surdimensionné pour un MVP,
  complexité d'intégration

### Option 5 : SOPS (Mozilla)

- **Avantage** : chiffrement de fichiers, intégration GitOps
- **Inconvénient** : pas de rotation dynamique, pas de transit encryption,
  pas d'API REST pour les applications

## Justification

| Critère | Vault | Azure KV | AWS SM | CyberArk | SOPS |
| ------- | ----- | -------- | ------ | -------- | ---- |
| Souveraineté | Self-hosted | Non (Azure) | Non (AWS) | Oui | Oui |
| Transit encryption | Oui | Non | Non | Oui | Non |
| Credentials dynamiques | Oui (DB, Redis) | Non | Partiel | Oui | Non |
| Rotation automatique | Oui | Partiel | Oui | Oui | Non |
| Audit trail | Oui (audit backend) | Oui | Oui | Oui | Non |
| Client .NET | VaultSharp (Apache-2.0) | SDK MS | SDK AWS | SDK tiers | CLI |
| Coût | OSS (self-hosted) | Pay-per-use | Pay-per-use | Licence | Gratuit |
| Conformité HDS | Oui | Risque | Risque | Oui | Partiel |

## Conséquences

### Positives

- Conformité HDS complète : rotation, audit, chiffrement
- Souveraineté totale : Vault hébergé sur OVHcloud, zéro dépendance cloud US
- Transit encryption pour le chiffrement applicatif via `Granit.Vault`
- Credentials dynamiques PostgreSQL : connexions éphémères, révocation automatique

### Négatives

- Complexité opérationnelle : déploiement, unseal, backup, upgrade de Vault
- VaultSharp est maintenu par un développeur individuel (risque de maintenance)
- Licence Vault BSL 1.1 : à surveiller si évolution des conditions

## Conditions de réévaluation

Ce choix devrait être réévalué si :

- Un service de secrets managé souverain émerge (OVHcloud KMS+ avec Transit, credentials dynamiques)
- HashiCorp modifie significativement la licence BSL de Vault
- VaultSharp perd sa maintenance (évaluer OpenBao comme alternative)

## Références

- Commit initial : `52f1444` (2026-02-21)
- Issue : [#9 — Granit.Vault](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/9)
- IAC ADR-003 : [Vault sur Kubernetes](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-003-vault-sur-kubernetes.md)
- IAC ADR-004 : [External Secrets Operator](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-004-external-secrets-operator.md)
- VaultSharp : <https://github.com/rajanadar/VaultSharp>
