# Configuration Vault en production

## Architecture

```mermaid
flowchart LR
    APP["Application Granit"] -->|Kubernetes Auth| VA["Vault Agent\n(sidecar)"]
    VA -->|Lease renewal| VC["Vault Cluster\n(Raft HA)"]

    VC -->|Dynamic credentials| PG["PostgreSQL"]
    VC -->|Transit encrypt/decrypt| APP
    VC -->|KV secrets| APP

    style APP fill:#4a9eff,color:#fff
    style VC fill:#e67e22,color:#fff
    style PG fill:#ff6b6b,color:#fff
```

## Authentification Kubernetes

En production, Granit s'authentifie auprès de Vault via le ServiceAccount
Kubernetes du pod :

```json
{
  "Vault": {
    "Address": "https://vault.internal:8200",
    "AuthMethod": "Kubernetes",
    "Kubernetes": {
      "Role": "guava-backend",
      "ServiceAccountTokenPath": "/var/run/secrets/kubernetes.io/serviceaccount/token"
    }
  }
}
```

### Flux d'authentification

```mermaid
sequenceDiagram
    participant POD as Pod Granit
    participant K8S as API Server K8s
    participant VAULT as Vault

    POD->>POD: Lit ServiceAccount JWT (/var/run/secrets/...)
    POD->>VAULT: POST /auth/kubernetes/login (jwt, role)
    VAULT->>K8S: TokenReview (vérifie le JWT)
    K8S-->>VAULT: JWT valide, ServiceAccount = guava-backend
    VAULT-->>POD: Vault token (TTL 1h, renewable)
    Note over POD,VAULT: Le token est renouvelé automatiquement par IVaultCredentialLeaseManager
```

## Credentials dynamiques PostgreSQL

Vault génère des identifiants PostgreSQL éphémères avec un TTL court.
Le `IVaultCredentialLeaseManager` de Granit gère automatiquement le cycle
de vie des leases.

### Configuration

```json
{
  "Vault": {
    "Database": {
      "MountPoint": "database",
      "RoleName": "guava-readonly",
      "LeaseTtlSeconds": 3600,
      "LeaseRenewalMarginSeconds": 300
    }
  }
}
```

### Cycle de vie d'un lease

```mermaid
stateDiagram-v2
    [*] --> Obtain: Démarrage application
    Obtain --> Active: Vault retourne credentials
    Active --> Renew: TTL - margin atteint
    Renew --> Active: Renewal OK
    Renew --> Obtain: Renewal échoué
    Active --> Revoke: Arrêt application
    Revoke --> [*]

    note right of Active: Credentials utilisés par EF Core
    note right of Renew: Automatique via IVaultCredentialLeaseManager
```

### Points clés

- **Pas de mot de passe statique** : les credentials sont générés dynamiquement
  et expirent automatiquement.
- **Rotation transparente** : le `IVaultCredentialLeaseManager` renouvelle le lease
  avant expiration. EF Core utilise les nouveaux credentials sans interruption.
- **Révocation au shutdown** : à l'arrêt du pod, les credentials sont révoqués
  pour limiter la fenêtre d'exposition.

## Transit Engine (chiffrement de champs)

Le Transit Engine chiffre et déchiffre des données sans exposer la clé de chiffrement.

### Configuration

```json
{
  "Vault": {
    "Transit": {
      "MountPoint": "transit",
      "KeyName": "granit-hds"
    }
  }
}
```

### Rotation de clé

La rotation de clé Transit est transparente :

```bash
# Rotation de la clé (Vault CLI ou API)
vault write -f transit/keys/granit-hds/rotate

# Les anciennes données restent lisibles
# La version de la clé est encodée dans le ciphertext (vault:v2:xxx...)
```

Après rotation, les nouvelles écritures utilisent la nouvelle version de clé.
Les anciennes données sont déchiffrées avec l'ancienne version (stockée dans
le ciphertext prefix).

## Monitoring Vault

| Métrique | Seuil d'alerte | Description |
| --- | --- | --- |
| Lease count | > 1000 | Nombre de leases actifs (fuite potentielle) |
| Token renewal failures | > 0 sur 5 min | Perte d'accès imminente |
| Seal status | sealed = true | Vault scellé — intervention requise |
| Storage backend latency | > 100ms | Dégradation du stockage Raft |

## Liens

- [Vault](../framework/security/vault.md)
- [Chiffrement](../framework/security/encryption.md)
- [Recette : chiffrer des données sensibles](../cookbook/chiffrement-donnees-sensibles.md)
