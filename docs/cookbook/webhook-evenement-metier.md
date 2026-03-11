# Publier un webhook sur un événement métier

## Problème

Quand un événement métier se produit (nouveau patient, rendez-vous confirmé),
il faut notifier des systèmes externes via HTTP POST avec garantie de livraison,
signature HMAC pour l'authenticité, et audit trail ISO 27001.

## Solution

### Publier l'événement

```csharp
using Granit.Webhooks;
using Wolverine;

namespace MyApp.Handlers;

public static class PatientCreatedHandler
{
    public static async Task HandleAsync(
        PatientCreatedEvent evt,
        IWebhookPublisher webhookPublisher,
        CancellationToken cancellationToken)
    {
        await webhookPublisher.PublishAsync(
            eventType: "patient.created",
            payload: new
            {
                PatientId = evt.PatientId,
                CreatedAt = evt.CreatedAt
            },
            ct);
    }
}
```

### Enregistrer un abonnement

```csharp
// Via l'API d'administration ou en base de données
WebhookSubscription subscription = new()
{
    EventType = "patient.created",
    TargetUrl = "https://partner.example.com/webhooks/granit",
    Secret = "hmac-secret-shared-with-partner",
    IsActive = true
};
```

### Vérifier côté récepteur

Le récepteur vérifie la signature HMAC-SHA256 :

```csharp
// Côté partenaire (pas dans Granit)
string signature = request.Headers["X-Webhook-Signature"];
string payload = await new StreamReader(request.Body).ReadToEndAsync();

string expected = ComputeHmacSha256(payload, sharedSecret);
bool isValid = CryptographicOperations.FixedTimeEquals(
    Encoding.UTF8.GetBytes(signature),
    Encoding.UTF8.GetBytes(expected));
```

## Explication

```mermaid
sequenceDiagram
    participant H as PatientCreatedHandler
    participant WP as IWebhookPublisher
    participant FF as WebhookFanoutHandler
    participant DB as Outbox PostgreSQL
    participant S1 as SendWebhookHandler (Partner A)
    participant S2 as SendWebhookHandler (Partner B)
    participant P1 as Partner A
    participant P2 as Partner B

    H->>WP: PublishAsync("patient.created", payload)
    WP->>DB: INSERT fanout message (Outbox)

    Note over DB,FF: Dispatch asynchrone (at-least-once)

    DB->>FF: WebhookFanoutMessage
    FF->>FF: Résout les abonnements actifs
    FF->>S1: SendWebhookCommand (Partner A)
    FF->>S2: SendWebhookCommand (Partner B)

    S1->>P1: POST /webhooks (payload + X-Webhook-Signature)
    P1-->>S1: 200 OK
    Note over S1: Audit trail enregistré

    S2->>P2: POST /webhooks (payload + X-Webhook-Signature)
    P2-->>S2: 500 Error
    Note over S2: Retry automatique (backoff exponentiel)
```

### Points clés

- **Fan-out** : `IWebhookPublisher` crée un message de fan-out. Le
  `WebhookFanoutHandler` résout tous les abonnements actifs pour cet
  `eventType` et crée un `SendWebhookCommand` par abonnement.
- **Outbox transactionnel** : le message de fan-out est persisté dans
  la même transaction que l'événement métier. Pas de perte de webhook.
- **Signature HMAC-SHA256** : chaque webhook est signé avec le secret
  partagé de l'abonnement. Le header `X-Webhook-Signature` permet au
  récepteur de vérifier l'authenticité et l'intégrité.
- **Retry** : en cas d'échec HTTP (4xx/5xx), Wolverine relance avec
  backoff exponentiel. Après épuisement des tentatives, le message
  est envoyé en Dead Letter Queue.
- **Audit trail** : chaque tentative d'envoi est enregistrée (statut HTTP,
  durée, erreur éventuelle) pour conformité ISO 27001.

### Configuration

```json
{
  "Webhooks": {
    "MaxRetryAttempts": 5,
    "RetryDelaySeconds": [10, 30, 120, 600, 3600],
    "SignatureHeaderName": "X-Webhook-Signature",
    "TimeoutSeconds": 30
  }
}
```

## Liens

- [Webhooks](../framework/messaging/webhooks.md)
- [Wolverine](../framework/messaging/wolverine.md)
- [Pattern Transactional Outbox](../patterns/cloud-saas/transactional-outbox.md)
