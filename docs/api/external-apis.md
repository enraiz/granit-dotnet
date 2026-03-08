# API externes

Ce document référence toutes les API et services externes appelés par les packages
Granit, leur authentification, leur configuration et les politiques de résilience
appliquées.

## Vue d'ensemble

| Service | Package(s) | Protocole | Auth | Résilience | Timeout |
| --- | --- | --- | --- | --- | --- |
| Brevo | `Granit.Notifications.Brevo` | HTTPS REST | API key (header) | Standard (retry + circuit breaker) + Wolverine | Configurable (défaut 30 s) |
| Keycloak Admin | `Granit.Identity.Keycloak` | HTTPS REST | OAuth 2.0 client\_credentials | Standard (retry + circuit breaker) | Configurable (défaut 30 s) |
| SMTP | `Granit.Notifications.Email.Smtp` | TCP/TLS | Username/Password | Wolverine (via handler) | Configurable (défaut 30 s) |
| Web Push (VAPID) | `Granit.Notifications.Push` | HTTPS | VAPID (P-256) | Wolverine (via handler) | PushServiceClient défaut |
| Vault | `Granit.Vault` | HTTPS | Kubernetes JWT ou Token | Lease renewal avec fallback | CancellationToken caller |
| S3 | `Granit.BlobStorage.S3` | HTTPS | AccessKey/SecretKey | AWS SDK intégré | AWS SDK défaut |
| Webhook delivery | `Granit.Webhooks` | HTTPS | HMAC signature | Wolverine 6 niveaux (30 s → 12 h) | Configurable |
| OTLP | `Granit.Observability` | gRPC/HTTP | Aucune (réseau interne) | SDK OpenTelemetry batch | SDK défaut |

## Brevo (Email, SMS, WhatsApp)

**Package** : `Granit.Notifications.Brevo`

**URL de base** : `https://api.brevo.com/v3` (configurable via `BrevoOptions.BaseUrl`)

### Endpoints utilisés

| Canal | Méthode | Endpoint | Description |
| --- | --- | --- | --- |
| Email | POST | `/smtp/email` | Envoi d'email transactionnel |
| SMS | POST | `/transactionalSMS/sms` | Envoi de SMS transactionnel |
| WhatsApp | POST | `/whatsapp/sendTemplate` | Envoi de template WhatsApp |

### Authentification

Header `api-key` avec la clé API Brevo. La clé doit être chargée depuis Vault — jamais
en clair dans `appsettings.json`.

### Configuration

```json
{
  "Notifications:Brevo": {
    "ApiKey": "xkeysib-...",
    "DefaultSenderEmail": "noreply@example.com",
    "DefaultSenderName": "Mon App",
    "DefaultSmsSenderId": "MonApp",
    "BaseUrl": "https://api.brevo.com/v3",
    "TimeoutSeconds": 30
  }
}
```

### Résilience

- **Niveau transport** : `AddStandardResilienceHandler()` (Microsoft.Extensions.Http.Resilience)
  - Retry avec backoff exponentiel sur erreurs transitoires (429, 5xx)
  - Circuit breaker (coupe les appels si trop d'échecs consécutifs)
  - Timeout par tentative (30 s par défaut)
- **Niveau applicatif** : Wolverine `NotificationDeliveryHandler` catch les exceptions
  et déclenche un retry avec backoff configurable
- **Erreurs** : le body d'erreur Brevo est parsé et inclus dans l'exception et les logs

### Codes d'erreur courants

| Code | Signification | Action |
| --- | --- | --- |
| 400 | Paramètre invalide (email, payload) | Pas de retry — erreur permanente |
| 401 | Clé API invalide ou expirée | Vérifier la rotation Vault |
| 402 | Crédits insuffisants (SMS/WhatsApp) | Pas de retry — recharger le compte |
| 429 | Rate limit atteint | Retry automatique (backoff) |
| 5xx | Erreur serveur Brevo | Retry automatique (backoff) |

## Keycloak Admin API

**Package** : `Granit.Identity.Keycloak`

**URL de base** : `{BaseUrl}/admin/realms/{Realm}/` (configurable via `KeycloakAdminOptions`)

### Endpoints utilisés

| Opération | Méthode | Endpoint |
| --- | --- | --- |
| Lister les utilisateurs | GET | `/admin/realms/{realm}/users` |
| Obtenir un utilisateur | GET | `/admin/realms/{realm}/users/{id}` |
| Créer un utilisateur | POST | `/admin/realms/{realm}/users` |
| Modifier un utilisateur | PUT | `/admin/realms/{realm}/users/{id}` |
| Activer/désactiver | PUT | `/admin/realms/{realm}/users/{id}` |
| Sessions utilisateur | GET | `/admin/realms/{realm}/users/{id}/sessions` |
| Terminer une session | DELETE | `/admin/realms/{realm}/sessions/{sessionId}` |
| Déconnecter tout | POST | `/admin/realms/{realm}/users/{id}/logout` |
| Credentials | GET | `/admin/realms/{realm}/users/{id}/credentials` |
| Rôles realm | GET | `/admin/realms/{realm}/roles` |
| Rôles utilisateur | GET/POST/DELETE | `/admin/realms/{realm}/users/{id}/role-mappings/realm` |
| Groupes | GET | `/admin/realms/{realm}/groups` |
| Groupes utilisateur | GET/PUT/DELETE | `/admin/realms/{realm}/users/{id}/groups/{groupId}` |
| Reset password | PUT | `/admin/realms/{realm}/users/{id}/reset-password` |
| Token (service account) | POST | `/realms/{realm}/protocol/openid-connect/token` |
| Vérification credentials | POST | `/realms/{realm}/protocol/openid-connect/token` |

### Authentification

OAuth 2.0 `client_credentials` grant via un service account Keycloak. Le token est
mis en cache avec un SemaphoreSlim (thread-safe) et renouvelé 30 secondes avant
expiration.

### Configuration

```json
{
  "KeycloakAdmin": {
    "BaseUrl": "https://keycloak.example.com",
    "Realm": "my-realm",
    "ClientId": "granit-service-account",
    "ClientSecret": "secret-from-vault",
    "TimeoutSeconds": 30,
    "UseTokenExchangeForDeviceActivity": false,
    "DirectAccessClientId": null
  }
}
```

### Résilience

- **Niveau transport** : `AddStandardResilienceHandler()` — retry, circuit breaker, timeout
- **Lectures** : dégradation gracieuse (retour vide/null + log warning)
- **Écritures** : propagation de l'exception au caller
- **Token exchange** : RFC 8693 pour l'impersonation (optionnel, `UseTokenExchangeForDeviceActivity`)

### Rôles requis sur le service account

| Rôle | Opérations |
| --- | --- |
| `realm-management:view-users` | Toutes les lectures |
| `realm-management:manage-users` | Écritures (enable/disable, rôles, password, création, groupes) |
| `realm-management:impersonation` | Token exchange pour device activity |

## SMTP (MailKit)

**Package** : `Granit.Notifications.Email.Smtp`

**Protocole** : TCP/TLS (STARTTLS ou connexion directe selon configuration)

### Configuration

```json
{
  "Notifications:Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "UseSsl": true,
    "Username": "smtp-user",
    "Password": "from-vault",
    "TimeoutSeconds": 30
  }
}
```

### Résilience

- **Timeout** : configurable via `SmtpOptions.TimeoutSeconds` (défaut 30 s), appliqué
  sur le `SmtpClient.Timeout` de MailKit (connexion + envoi)
- **Retry** : géré par Wolverine via `NotificationDeliveryHandler`
- **Logging** : `[LoggerMessage]` source-generated pour chaque envoi réussi

## Web Push (VAPID / W3C)

**Package** : `Granit.Notifications.Push`

**Protocole** : HTTPS vers les endpoints push des navigateurs (Firebase, Mozilla, Apple)

### Authentification

VAPID (Voluntary Application Server Identification) avec une paire de clés P-256 :

- Clé publique : partagée avec le frontend lors de l'abonnement
- Clé privée : utilisée côté serveur pour signer les requêtes

### Configuration

```json
{
  "Notifications:Push": {
    "VapidPublicKey": "BN...",
    "VapidPrivateKey": "from-vault",
    "VapidSubject": "mailto:admin@example.com"
  }
}
```

### Résilience

- **HTTP 410 Gone** : l'abonnement est automatiquement supprimé du store
- **Autres erreurs** : accumulées (sans interrompre les autres envois) et remontées
  en `AggregateException` pour déclenchement du retry Wolverine
- **Retry** : géré par Wolverine via `NotificationDeliveryHandler`

## Vault (HashiCorp)

**Package** : `Granit.Vault`

**Bibliothèque** : VaultSharp 1.17+

### Opérations

| Service | Méthode VaultSharp | Usage |
| --- | --- | --- |
| Transit encrypt | `V1.Secrets.Transit.EncryptAsync` | Chiffrement de données sensibles |
| Transit decrypt | `V1.Secrets.Transit.DecryptAsync` | Déchiffrement de données sensibles |
| Database credentials | `V1.Secrets.Database.GetCredentialsAsync` | Credentials dynamiques PostgreSQL |
| Lease renewal | `V1.System.RenewLeaseAsync` | Renouvellement des leases de credentials |
| Health check | `V1.System.GetHealthStatusAsync` | Probe de santé |

### Authentification

Deux modes :

- **Kubernetes** (production) : JWT lu depuis le fichier ServiceAccount, échangé
  contre un token Vault via le auth backend Kubernetes
- **Token** (dev uniquement) : token statique — un warning est loggé au démarrage

### Résilience

- **Lease renewal** : `VaultCredentialLeaseManager` renouvelle les leases avant
  expiration. En cas d'échec, obtient de nouvelles credentials automatiquement
- **Timeout** : `.WaitAsync(cancellationToken)` sur chaque appel VaultSharp
- **Sécurité** : les tokens et secrets ne sont jamais exposés dans les logs ou
  les health checks

## S3 (OVHcloud Object Storage)

**Package** : `Granit.BlobStorage.S3`

**Bibliothèque** : AWSSDK.S3

### Opérations

- Upload/Download via presigned URLs (pas d'appel serveur direct)
- `DeleteObjectAsync` — suppression de blobs
- `GetObjectMetadataAsync` — métadonnées
- `GetObjectAsync` — lecture avec range partiel

### Configuration

```json
{
  "BlobStorage:S3": {
    "ServiceUrl": "https://s3.rbx.io.cloud.ovh.net",
    "Region": "rbx",
    "ForcePathStyle": true,
    "AccessKey": "from-vault",
    "SecretKey": "from-vault"
  }
}
```

### Résilience

- **AWS SDK intégré** : retry avec backoff exponentiel (configurable via `AmazonS3Config`)
- **Souveraineté** : endpoint OVHcloud (FR) uniquement — jamais AWS/Azure/GCP pour
  les données de santé (contrainte HDS)

## Webhook delivery (sortant)

**Package** : `Granit.Webhooks`

**Protocole** : HTTPS POST vers des endpoints configurés par les utilisateurs

### Authentification

Signature HMAC-SHA256 dans le header `x-granit-signature`, calculée à partir du body
JSON et d'un secret partagé (stocké chiffré via `IWebhookSecretProtector`).

Headers envoyés :

- `x-granit-signature` — signature HMAC
- `x-granit-event-id` — UUID de l'événement
- `x-granit-event-type` — type de l'événement

### Résilience

Modèle de référence du projet :

- **Timeout** : configurable via `WebhooksOptions.HttpTimeoutSeconds`
- **Classification d'erreurs** :
  - Non-retriable (400, 401, 403, 404, 405, 410, 422) → échec enregistré, pas de retry
  - Retriable (429, 5xx, timeout réseau) → `WebhookDeliveryException` → retry Wolverine
- **Retry Wolverine** : 6 niveaux d'exponential backoff (30 s → 2 min → 10 min →
  30 min → 2 h → 12 h → Dead-Letter Queue)
- **Auto-suspension** : les subscriptions qui reçoivent 401/403/404/410 sont
  automatiquement suspendues
- **Audit** : chaque tentative (succès/échec) est enregistrée dans le delivery store

## OTLP (OpenTelemetry Collector)

**Package** : `Granit.Observability`

**Protocole** : gRPC ou HTTP/Protobuf vers un collector OTLP

### Configuration

```json
{
  "Observability": {
    "OtlpEndpoint": "http://otel-collector:4317",
    "ServiceName": "mon-service",
    "TracingEnabled": true,
    "MetricsEnabled": true
  }
}
```

### Résilience

- **SDK OpenTelemetry** : batch export avec retry intégré, buffer en mémoire
- **Dégradation gracieuse** : si le collector est indisponible, les données sont
  perdues silencieusement (pas de crash applicatif)
