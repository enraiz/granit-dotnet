# Timeline — Granit.Timeline

Flux d'activité unifié (inspiré Odoo Chatter) pour les entités métier
des applications Digital Dynamics. Fusionne historique technique (logs système,
transitions) et communication humaine (commentaires, notes internes, pièces jointes).

| Package | Rôle |
| --- | --- |
| `Granit.Timeline` | Entités, interfaces, implémentations in-memory, MentionParser |
| `Granit.Timeline.EntityFrameworkCore` | DbContext, configurations EF Core, stores PostgreSQL |
| `Granit.Timeline.Notifications` | Pont followers/notifications vers `Granit.Notifications` |
| `Granit.Timeline.Endpoints` | API REST Minimal API (flux, entrées, followers) |

## Architecture

```text
ITimelineStoreWriter            ← écriture (PostEntry, Delete, AddAttachment)
ITimelineStoreReader            ← lecture (flux paginé, chronologique desc.)
ITimelineFollowerService        ← gestion des abonnés (follow/unfollow)
ITimelineNotifier               ← fan-out notifications (commentaires, @mentions)
MentionParser                   ← extraction @[Nom](user:guid) du Markdown
```

## Concepts clés

### Marqueur ITimelined

Les entités qui souhaitent activer le flux d'activité implémentent l'interface
marqueur `ITimelined` (zéro membre, opt-in pur) :

```csharp
public class Patient : AuditedEntity, ITimelined
{
    public string Name { get; set; } = string.Empty;
}
```

Pour surcharger le nom du type d'entité dans le flux :

```csharp
[Timelined("Dossier Patient")]
public class Patient : AuditedEntity, ITimelined { }
```

### Types d'entrées

Trois types coexistent dans la même table `timeline_entries` :

| Type | Usage | Modifiable | Supprimable |
| --- | --- | --- | --- |
| `Comment` | Commentaire humain, visible par les followers | Non | Oui (soft-delete RGPD) |
| `SystemLog` | Log auto-généré (audit) | Non | Non (INSERT-only, HDS) |
| `InternalNote` | Note interne, visible uniquement par le staff | Non | Oui (soft-delete RGPD) |

La suppression d'un `SystemLog` lève une `InvalidOperationException` — conformité
HDS (piste d'audit immutable, rétention 3 ans minimum).

### Pièces jointes

Les pièces jointes sont liées aux entrées via `TimelineAttachment`. Le `BlobId`
est une référence opaque vers `Granit.BlobStorage` (aucune dépendance dure).

La sécurité des pièces jointes est **héritée** de l'entité parente : l'endpoint
vérifie l'autorisation sur `(EntityType, EntityId)` avant de retourner l'URL
pré-signée.

### Réponses threadées

Le champ `ParentEntryId` permet les réponses en fil de discussion. L'API retourne
une liste plate triée chronologiquement ; le composant React `<Timeline />`
est responsable de l'indentation côté frontend.

## @Mentions

Format Markdown : `@[Dr. Martin](user:550e8400-e29b-41d4-a716-446655440000)`

Le `MentionParser` extrait les GUIDs valides. Le workflow complet lors d'un POST :

1. Persister l'entrée via `ITimelineStoreWriter.PostEntryAsync()`
2. Extraire les @mentions via `MentionParser.ExtractMentionedUserIds()`
3. Auto-abonner les utilisateurs mentionnés via `ITimelineFollowerService.FollowAsync()`
4. Notifier les followers via `ITimelineNotifier.NotifyEntryPostedAsync()`
5. Notifier les mentionnés via `ITimelineNotifier.NotifyMentionedUsersAsync()`

## Dégradation gracieuse

| Dépendance | Présente | Absente |
| --- | --- | --- |
| `Granit.Notifications` | Followers via `INotificationSubscriptionStore`, fan-out | `InMemoryTimelineFollowerService`, `NullTimelineNotifier` |
| `Granit.BlobStorage` | URLs pré-signées pour les pièces jointes | Métadonnées uniquement |
| `Granit.Workflow` | Transitions fusionnées dans le flux (Phase 2) | Flux = entrées timeline uniquement |
| Multi-tenancy | `TenantId` automatique, filtres requêtes | `TenantId = null` |

## Installation

### Package de base (in-memory)

```csharp
services.AddGranitTimeline();
```

### Avec persistance EF Core

```csharp
builder.AddGranitTimelineEntityFrameworkCore(opts =>
    opts.UseNpgsql(connectionString));
```

### Avec notifications

```csharp
services.AddGranitTimelineNotifications();
```

### Endpoints REST

```csharp
app.MapTimelineEndpoints();

// Avec préfixe de domaine personnalisé :
app.MapTimelineEndpoints(opts => opts.RoutePrefix = "admin/timeline");
```

## API REST

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/{entityType}/{entityId}` | Flux paginé (skip, take) |
| `POST` | `/{entityType}/{entityId}/entries` | Poster commentaire/note |
| `DELETE` | `/{entityType}/{entityId}/entries/{id}` | Soft-delete (RGPD) |
| `POST` | `/{entityType}/{entityId}/follow` | S'abonner à l'entité |
| `DELETE` | `/{entityType}/{entityId}/follow` | Se désabonner |
| `GET` | `/{entityType}/{entityId}/followers` | Lister les abonnés |

### Exemple de requête POST

```json
{
  "entryType": 0,
  "body": "Bonjour @[Dr. Martin](user:550e8400-e29b-41d4-a716-446655440000), merci de vérifier.",
  "parentEntryId": null,
  "attachmentBlobIds": ["a1b2c3d4-..."]
}
```

### Exemple de réponse GET (flux paginé)

```json
{
  "items": [
    {
      "id": "...",
      "occurredAt": "2026-02-28T10:30:00Z",
      "entryType": 0,
      "authorId": "user-1",
      "authorName": "Dr. Dupont",
      "body": "Commentaire avec @mention",
      "attachments": [
        {
          "id": "...",
          "blobId": "...",
          "fileName": "rapport.pdf",
          "contentType": "application/pdf",
          "sizeBytes": 2048
        }
      ],
      "parentEntryId": null
    }
  ],
  "totalCount": 42
}
```

## Schéma EF Core

### Table `timeline_entries`

Index principal : `(EntityType, EntityId, TenantId, CreatedAt DESC)` — optimisé
pour la requête de flux paginé.

Filtre global soft-delete : les entrées supprimées sont automatiquement exclues
des requêtes standard.

### Table `timeline_attachments`

Index : `(EntryId)` — recherche rapide des pièces jointes par entrée.

## Types de notifications

| Type | Nom | Canaux par défaut |
| --- | --- | --- |
| `TimelineCommentNotificationType` | `timeline.comment_posted` | InApp, SignalR |
| `TimelineMentionNotificationType` | `timeline.user_mentioned` | InApp, SignalR, Email |

L'auteur est automatiquement exclu des destinataires de ses propres notifications.

## Conformité HDS / RGPD

- **HDS** : Les entrées `SystemLog` sont INSERT-only immutables. La suppression
  lève `InvalidOperationException`. Rétention 3 ans minimum.
- **RGPD** : Les entrées `Comment` et `InternalNote` supportent le soft-delete
  (droit à l'effacement). Les données sont marquées comme supprimées mais restent
  disponibles pour la piste d'audit.
- **Souveraineté** : La base de données doit être hébergée en Europe
  (OVHcloud FR). Ne jamais utiliser AWS/Azure/GCP pour les données de santé.
