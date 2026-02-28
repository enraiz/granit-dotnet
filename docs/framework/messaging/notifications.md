# Messagerie — Granit.Notifications

Moteur de notifications **multi-canal** pour les applications Digital Dynamics.
Publie des notifications aux utilisateurs via InApp, SignalR, Email, SMS, WhatsApp et Web Push.
Basé sur Wolverine (Outbox at-least-once), conforme HDS (audit trail immuable) et RGPD.

Dix packages composables :

| Package | Rôle |
| --- | --- |
| `Granit.Notifications` | Core : abstractions, définitions, fan-out Wolverine, canal InApp, stores InMemory |
| `Granit.Notifications.EntityFrameworkCore` | Stores durables PostgreSQL + intercepteur de suivi d'entités |
| `Granit.Notifications.SignalR` | Temps réel browser via hub SignalR + Redis backplane K8s |
| `Granit.Notifications.Endpoints` | API REST Minimal API (inbox, préférences, followers) |
| `Granit.Notifications.Email` | Abstraction `IEmailSender` + canal Email (Keyed Services) |
| `Granit.Notifications.Email.Smtp` | Provider MailKit SMTP (clé `"Smtp"`) |
| `Granit.Notifications.Sms` | Abstraction `ISmsSender` + canal SMS (Keyed Services) |
| `Granit.Notifications.WhatsApp` | Abstraction `IWhatsAppSender` + canal WhatsApp (templates Meta pré-approuvés) |
| `Granit.Notifications.Brevo` | Provider unifié Email + SMS + WhatsApp via API Brevo |
| `Granit.Notifications.Push` | Web Push W3C VAPID (souveraineté, pas de FCM/APNs) |

## Architecture

```text
[Code applicatif]
      |
INotificationPublisher.PublishAsync(...)
      |
NotificationTrigger             <-- message publié dans l'Outbox Wolverine
      |
NotificationFanoutHandler       <-- résout destinataires + préférences + canaux
      |                              produit N commandes (1 par destinataire x canal)
      | (cascade Wolverine — même transaction Outbox)
      |
DeliverNotificationCommand x N  <-- enqueue dans la queue "notification-delivery"
      |
NotificationDeliveryHandler     <-- route vers le INotificationChannel correspondant
      |
INotificationChannel.SendAsync  <-- InApp | SignalR | Email | SMS | WhatsApp | Push
```

Le fan-out et la livraison sont **entièrement découplés** : Wolverine publie les `N`
commandes dans l'Outbox avant de rendre la main à l'application. Les livraisons s'effectuent
en parallèle (jusqu'à `MaxParallelDeliveries`) avec une politique de retry exponentielle
durable.

### Résolution des destinataires

Le `NotificationFanoutHandler` résout les destinataires selon trois stratégies, par ordre
de priorité :

1. **Liste explicite** — `PublishAsync(type, data, recipientUserIds)` : les destinataires
   sont fournis directement par l'application.
2. **Followers d'entité** — `PublishToEntityFollowersAsync(type, data, entity)` : résolution
   via `INotificationSubscriptionStore.GetEntityFollowerIdsAsync()`.
3. **Abonnés au type** — `PublishToSubscribersAsync(type, data)` : résolution via
   `INotificationSubscriptionStore.GetSubscriberIdsAsync()`.

Pour chaque destinataire, le handler consulte `INotificationPreferenceStore` afin de
filtrer les canaux désactivés (sauf si `AllowUserOptOut = false` dans la définition).

## Installation rapide

### Module de base (stores en mémoire, développement/tests)

```csharp
[DependsOn(typeof(GranitNotificationsModule))]
public sealed class MyAppModule : GranitModule { }
```

### Module avec EF Core (production)

```csharp
[DependsOn(typeof(GranitNotificationsEntityFrameworkCoreModule))]
public sealed class MyAppModule : GranitModule { }
```

L'enregistrement EF Core se fait via l'extension `AddGranitNotificationsEntityFrameworkCore` :

```csharp
builder.AddGranitNotificationsEntityFrameworkCore(
    opts => opts.UseNpgsql(connectionString));
```

### Ajout des canaux

```csharp
// SignalR temps réel (avec Redis backplane pour K8s)
builder.Services.AddGranitNotificationsSignalR(redisConnectionString);

// Email via MailKit SMTP
builder.Services.AddGranitNotificationsEmail();
builder.Services.AddGranitNotificationsEmailSmtp();

// SMS
builder.Services.AddGranitNotificationsSms();

// WhatsApp
builder.Services.AddGranitNotificationsWhatsApp();

// Web Push VAPID
builder.Services.AddGranitNotificationsPush();

// Provider unifié Brevo (Email + SMS + WhatsApp)
builder.Services.AddGranitNotificationsBrevo();
```

### Endpoints REST

```csharp
app.MapGranitNotificationEndpoints();
```

### Configuration minimale

```json
{
  "Notifications": {
    "MaxParallelDeliveries": 8,
    "SignalR": {
      "RedisConnectionString": "redis:6379"
    },
    "Email": {
      "Provider": "Smtp",
      "SenderAddress": "noreply@example.com",
      "SenderName": "Mon Application"
    }
  }
}
```

## Définition des notifications

Chaque type de notification est déclaré via une classe dérivant de `NotificationType<TData>`.
Les instances sont des singletons applicatifs :

```csharp
public sealed class OrderShippedNotification : NotificationType<OrderShippedData>
{
    public static readonly OrderShippedNotification Instance = new();

    public override string Name => "Orders.Shipped";
    public override NotificationSeverity DefaultSeverity => NotificationSeverity.Info;
    public override IReadOnlyList<string> DefaultChannels =>
        [NotificationChannels.InApp, NotificationChannels.Email, NotificationChannels.Push];
}

public sealed record OrderShippedData
{
    public required string OrderId { get; init; }
    public required string TrackingNumber { get; init; }
}
```

### Enregistrement au démarrage

Les définitions sont enregistrées via un `INotificationDefinitionProvider` :

```csharp
public sealed class OrderNotificationDefinitionProvider : INotificationDefinitionProvider
{
    public void Define(INotificationDefinitionContext context)
    {
        context.Add(new NotificationDefinition("Orders.Shipped")
        {
            DisplayName = "Commande expédiée",
            Description = "Notification envoyée lorsqu'une commande est expédiée.",
            GroupName = "Commandes",
            DefaultChannels = [NotificationChannels.InApp, NotificationChannels.Email],
            AllowUserOptOut = true,
        });
    }
}

// Dans ConfigureServices :
services.AddNotificationDefinitions<OrderNotificationDefinitionProvider>();
```

### AllowUserOptOut

Le champ `AllowUserOptOut` contrôle si l'utilisateur peut désactiver la notification :

- `true` (défaut) — l'utilisateur peut opt-out par canal via ses préférences.
- `false` — la notification est toujours envoyée (alertes sécurité, brèches RGPD).

## Publication

`INotificationPublisher` est la façade applicative. Trois méthodes de publication :

### Destinataires explicites

```csharp
public sealed class OrderService(INotificationPublisher notifications)
{
    public async Task ShipOrderAsync(Order order, CancellationToken ct)
    {
        // ... logique métier ...

        await notifications.PublishAsync(
            OrderShippedNotification.Instance,
            new OrderShippedData
            {
                OrderId = order.Id.ToString(),
                TrackingNumber = order.TrackingNumber,
            },
            recipientUserIds: [order.CustomerId],
            ct);
    }
}
```

### Abonnés au type

```csharp
await notifications.PublishToSubscribersAsync(
    SystemMaintenanceNotification.Instance,
    new SystemMaintenanceData { ScheduledAt = maintenanceDate },
    ct);
```

### Followers d'entité (style Odoo)

```csharp
await notifications.PublishToEntityFollowersAsync(
    PatientStatusChangedNotification.Instance,
    new PatientStatusChangedData { NewStatus = "Active" },
    relatedEntity: new EntityReference("Patient", patient.Id.ToString()),
    ct);
```

L'implémentation `WolverineNotificationPublisher` sérialise les données en `JsonElement`
et publie un `NotificationTrigger` dans la queue Wolverine. Le tenant actif
(`ICurrentTenant`) est automatiquement propagé dans l'enveloppe.

## Canaux

### InApp

Canal intégré dans le package core. Persiste chaque notification dans `UserNotification`
via `IUserNotificationStore`. La base de données est la **source de vérité** ; les autres
canaux (Email, SMS, Push) sont des copies (principe Django).

Les notifications InApp sont consultées via l'API REST (inbox) ou le fil d'activité
(entity activity feed).

États possibles : `Unread` (0), `Read` (1).

### SignalR

Pousse les notifications en temps réel vers les clients browser connectés. Chaque
utilisateur est placé dans un groupe SignalR identifié par son `userId`.

Le hub `NotificationHub` gère automatiquement l'ajout/suppression des connexions aux groupes.
Le client reçoit l'événement `ReceiveNotification` avec un `SignalRNotificationMessage`.

En déploiement Kubernetes multi-pod, activer le **Redis backplane** :

```csharp
builder.Services.AddGranitNotificationsSignalR("redis:6379");
```

Le préfixe du canal Redis est `granit-notifications` pour éviter les collisions.

### Email

Résout le provider d'envoi à l'exécution via **.NET Keyed Services** (`EmailChannelOptions.Provider`).
Le canal utilise `IRecipientResolver` pour obtenir l'adresse email du destinataire.

Providers disponibles :

| Clé | Package | Implémentation |
| --- | --- | --- |
| `"Smtp"` | `Granit.Notifications.Email.Smtp` | `MailKitEmailSender` |
| `"Brevo"` | `Granit.Notifications.Brevo` | `BrevoNotificationProvider` |

### SMS

Résout le provider via Keyed Services (`SmsChannelOptions.Provider`). Utilise
`IRecipientResolver` pour obtenir le numéro de téléphone (format E.164).

Providers disponibles :

| Clé | Package | Implémentation |
| --- | --- | --- |
| `"Brevo"` | `Granit.Notifications.Brevo` | `BrevoNotificationProvider` |

### WhatsApp

Canal basé sur les **templates pré-approuvés Meta**. Le nom du template est dérivé du
`NotificationTypeName`. La langue est résolue dans l'ordre : `Culture` du trigger,
`PreferredCulture` du destinataire, puis `"fr"` par défaut.

Providers disponibles :

| Clé | Package | Implémentation |
| --- | --- | --- |
| `"Brevo"` | `Granit.Notifications.Brevo` | `BrevoNotificationProvider` |

### Push (Web Push W3C VAPID)

Implémentation conforme au standard W3C Push API avec authentification VAPID.
Pas de dépendance envers FCM (Google) ou APNs (Apple) : **souveraineté totale**.

Chaque utilisateur peut enregistrer plusieurs souscriptions browser (multi-onglet,
multi-appareil) via `IPushSubscriptionStore`. Les souscriptions expirées
(HTTP 410 Gone) sont automatiquement nettoyées.

La clé privée VAPID doit être stockée dans **Vault** en production.

## Architecture multi-provider (Keyed Services)

Les canaux Email, SMS et WhatsApp utilisent le pattern **.NET 8 Keyed Services** pour
découvrir le provider à l'exécution. Cela permet de changer de fournisseur par
configuration sans modifier le code :

```json
{
  "Notifications": {
    "Email": {
      "Provider": "Brevo",
      "SenderAddress": "noreply@example.com"
    },
    "Sms": {
      "Provider": "Brevo"
    },
    "WhatsApp": {
      "Provider": "Brevo"
    }
  }
}
```

Le package `Granit.Notifications.Brevo` enregistre une **implémentation unifiée**
(`BrevoNotificationProvider`) qui implémente simultanément `IEmailSender`, `ISmsSender`
et `IWhatsAppSender`, enregistrée sous la clé `"Brevo"` pour les trois interfaces.

Pour ajouter un nouveau provider, il suffit d'implémenter l'interface correspondante
et de l'enregistrer comme Keyed Service :

```csharp
services.AddKeyedSingleton<IEmailSender, SendGridEmailSender>("SendGrid");
```

Puis de configurer la clé dans `appsettings.json` :

```json
{
  "Notifications": {
    "Email": {
      "Provider": "SendGrid"
    }
  }
}
```

## Entity Tracking style Odoo

Le module offre un système de suivi d'entités inspiré du **chatter Odoo** : les
utilisateurs peuvent suivre une entité métier et recevoir automatiquement des
notifications lorsque des propriétés surveillées changent.

### ITrackedEntity

Implémenter `ITrackedEntity` sur les entités EF Core à surveiller :

```csharp
public sealed class Patient : Entity, ITrackedEntity
{
    public static string EntityTypeName => "Patient";
    public string GetEntityId() => Id.ToString();

    public string Status { get; set; } = string.Empty;
    public string AssignedDoctorId { get; set; } = string.Empty;

    public static IReadOnlyDictionary<string, TrackedPropertyConfig> TrackedProperties => new Dictionary<string, TrackedPropertyConfig>
    {
        ["Status"] = new()
        {
            NotificationTypeName = "Patient.StatusChanged",
            Severity = NotificationSeverity.Warning,
        },
        ["AssignedDoctorId"] = new()
        {
            NotificationTypeName = "Patient.DoctorAssigned",
            Severity = NotificationSeverity.Info,
        },
    };
}
```

### EntityTrackingInterceptor

L'intercepteur EF Core `EntityTrackingInterceptor` (fourni par
`Granit.Notifications.EntityFrameworkCore`) détecte automatiquement les modifications
sur les propriétés tracées lors du `SaveChangesAsync` et publie un
`EntityStateChangedData` aux followers de l'entité.

Le payload `EntityStateChangedData` contient :

| Propriété | Description |
| --- | --- |
| `EntityType` | Nom du type d'entité (ex. `"Patient"`) |
| `EntityId` | Identifiant de l'entité |
| `PropertyName` | Nom de la propriété modifiée |
| `OldValue` | Ancienne valeur (sérialisée en string) |
| `NewValue` | Nouvelle valeur |
| `ChangedAt` | Date-heure de la modification |
| `ChangedByUserId` | Identifiant de l'utilisateur ayant effectué la modification |

### Followers

Les utilisateurs suivent/ne suivent plus une entité via `INotificationSubscriptionStore` :

```csharp
await subscriptionStore.FollowEntityAsync(userId, "Patient", patientId, tenantId, ct);
await subscriptionStore.UnfollowEntityAsync(userId, "Patient", patientId, tenantId, ct);
```

Ou via les endpoints REST :

- `POST /notifications/entity/{entityType}/{entityId}/follow`
- `DELETE /notifications/entity/{entityType}/{entityId}/follow`

## API REST

Les endpoints sont mappés via `app.MapGranitNotificationEndpoints()`. Tous les endpoints
requièrent une authentification.

### Inbox

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/notifications` | Liste paginée des notifications de l'utilisateur |
| `GET` | `/notifications/unread/count` | Nombre de notifications non lues |
| `POST` | `/notifications/{id}/read` | Marquer une notification comme lue |
| `POST` | `/notifications/read-all` | Marquer toutes les notifications comme lues |

### Fil d'activité (Activity Feed)

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/notifications/entity/{entityType}/{entityId}` | Notifications liées à une entité |

### Préférences

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/notifications/preferences` | Préférences de l'utilisateur |
| `PUT` | `/notifications/preferences` | Modifier une préférence (type + canal) |
| `GET` | `/notifications/types` | Liste des types de notification enregistrés |

### Abonnements

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/notifications/subscriptions` | Abonnements de l'utilisateur |
| `POST` | `/notifications/subscriptions/{typeName}` | S'abonner à un type |
| `DELETE` | `/notifications/subscriptions/{typeName}` | Se désabonner d'un type |

### Entity Followers

| Méthode | Route | Description |
| --- | --- | --- |
| `POST` | `/notifications/entity/{entityType}/{entityId}/follow` | Suivre une entité |
| `DELETE` | `/notifications/entity/{entityType}/{entityId}/follow` | Ne plus suivre |
| `GET` | `/notifications/entity/{entityType}/{entityId}/followers` | Liste des followers |

## Multi-tenancy

`NotificationFanoutHandler` et `WolverineNotificationPublisher` respectent la règle de
**soft dependency** sur `ICurrentTenant` (défini dans `Granit.Core.MultiTenancy`) :

1. Si `ICurrentTenant.IsAvailable == true` : utilise `ICurrentTenant.Id` (contexte HTTP
   ou background).
2. Sinon : utilise `NotificationTrigger.TenantId` (passé explicitement par l'émetteur),
   ou `null` si aucun tenant n'est actif.

Le module fonctionne avec ou sans `GranitMultiTenancyModule` installé. Un `NullTenantContext`
(`IsAvailable = false`) est enregistré par défaut dans `Granit.Core`.

Toutes les entités du domaine (`UserNotification`, `NotificationSubscription`,
`NotificationPreference`) implémentent `IMultiTenant` pour garantir l'isolation des données
par tenant.

Les endpoints REST résolvent le `TenantId` via `ICurrentTenant` pour filtrer les données.

## Configuration

Exemple complet de configuration `appsettings.json` :

```json
{
  "Notifications": {
    "MaxParallelDeliveries": 8,

    "SignalR": {
      "RedisConnectionString": "redis:6379"
    },

    "Email": {
      "Provider": "Brevo",
      "SenderAddress": "noreply@example.com",
      "SenderName": "Mon Application HDS"
    },

    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "UseSsl": true,
      "Username": "api-user",
      "Password": "vault://secret/smtp-password"
    },

    "Sms": {
      "Provider": "Brevo",
      "SenderId": "MonApp"
    },

    "WhatsApp": {
      "Provider": "Brevo"
    },

    "Push": {
      "VapidSubject": "mailto:admin@example.com",
      "VapidPublicKey": "BBase64UrlSafe...",
      "VapidPrivateKey": "vault://secret/vapid-private-key"
    },

    "Brevo": {
      "ApiKey": "vault://secret/brevo-api-key",
      "DefaultSenderEmail": "noreply@example.com",
      "DefaultSenderName": "Mon Application",
      "DefaultSmsSenderId": "MonApp",
      "BaseUrl": "https://api.brevo.com/v3"
    }
  }
}
```

> **HDS / Sécurité :** les valeurs sensibles (`ApiKey`, `VapidPrivateKey`, `Password`)
> doivent être injectées depuis **Vault** en production. Ne jamais les stocker en clair
> dans les fichiers de configuration.

### NotificationsOptions (`"Notifications"`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `MaxParallelDeliveries` | `int` | `8` | Parallélisme de la queue `notification-delivery` |

## Politique de retry

Les erreurs de livraison lèvent une `NotificationDeliveryException`. Wolverine la capture
et replanifie la livraison selon le calendrier exponentiel suivant :

| Tentative | Délai d'attente | Temps écoulé (total) |
| --- | --- | --- |
| 1 | 10 secondes | 10 s |
| 2 | 1 minute | ~1 min 10 s |
| 3 | 5 minutes | ~6 min 10 s |
| 4 | 30 minutes | ~36 min 10 s |
| 5 | 2 heures | ~2 h 36 min |

Après épuisement des 5 tentatives, le message est déplacé dans la **Dead Letter Queue**
Wolverine pour investigation manuelle. Le store `INotificationDeliveryStore` conserve
chaque tentative (succès ou échec).

> **Pourquoi Wolverine plutôt que Polly ?**
> Polly travaille en mémoire : un crash entre deux tentatives perd la livraison.
> Wolverine persiste chaque replanification dans l'Outbox PostgreSQL, ce qui garantit
> la livraison at-least-once conforme HDS même après un redémarrage de l'application.

## Observabilité

### ActivitySource

Le handler de livraison utilise `System.Diagnostics.Stopwatch` pour mesurer la durée
de chaque livraison. Chaque tentative (succès ou échec) est enregistrée dans
`NotificationDeliveryAttempt` avec la durée en millisecondes.

### Logs structurés

Le module utilise `ILogger` avec des logs structurés pour faciliter le requêtage dans
Loki :

- `LogDebug` — livraison réussie (canal, deliveryId, notificationId).
- `LogWarning` — canal non enregistré (graceful degradation) ou échec de livraison.

Exemple de requête LogQL :

```text
{app="mon-application"} |= "NotificationDeliveryHandler" | json | ChannelName="Email"
```

### Audit trail HDS

L'entité `NotificationDeliveryAttempt` est **INSERT-only** : aucune donnée d'audit ne
peut être modifiée ou supprimée. En mode InMemory (`NullNotificationDeliveryStore`),
l'audit est désactivé. En production avec EF Core (`EfCoreNotificationDeliveryStore`),
chaque tentative est persistée dans PostgreSQL.

Propriétés enregistrées :

| Propriété | Description |
| --- | --- |
| `DeliveryId` | Identifiant unique de la livraison |
| `NotificationId` | Identifiant de la notification source |
| `NotificationTypeName` | Type de notification |
| `ChannelName` | Canal utilisé |
| `RecipientUserId` | Destinataire |
| `TenantId` | Tenant concerné |
| `OccurredAt` | Date-heure de la tentative |
| `DurationMs` | Durée en millisecondes |
| `ErrorMessage` | Message d'erreur (null si succès) |
| `IsSuccess` | Résultat de la tentative |

## Conformité HDS

- L'Outbox Wolverine garantit la livraison **at-least-once** sans perte en cas de crash
- `NotificationDeliveryAttempt` est INSERT-only : aucune donnée d'audit ne peut être modifiée
- Les logs structurés incluent `NotificationId`, `ChannelName`, `RecipientUserId` — **jamais
  de PII** (email, téléphone, contenu de la notification)
- Les secrets (clés VAPID, credentials SMTP, clés API Brevo) doivent être **chiffrés via Vault**
- L'infrastructure doit rester **en Europe** (OVHcloud FR) — jamais sur AWS/Azure/GCP
- Conservation des `NotificationDeliveryAttempt` : **3 ans minimum** (politique de purge applicative)

## Dépendances Granit

| Direction | Modules |
| --- | --- |
| **Dépend de** | `Granit.Core`, `Granit.Timing`, `Granit.Wolverine` |
| **Utilisé par** | `Granit.Notifications.EntityFrameworkCore`, `Granit.Notifications.SignalR`, `Granit.Notifications.Endpoints`, `Granit.Notifications.Email`, `Granit.Notifications.Sms`, `Granit.Notifications.WhatsApp`, `Granit.Notifications.Push` |

> Voir le [graphe de dépendances complet](../dependencies.md).
