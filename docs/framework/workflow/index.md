# Workflow — Granit.Workflow

Machine à états finis (FSM) générique pour la gestion du cycle de vie des entités
métier dans les applications Digital Dynamics.

| Package | Rôle |
| --- | --- |
| `Granit.Workflow` | Moteur FSM générique, définitions fluent, événements domaine |
| `Granit.Workflow.EntityFrameworkCore` | Intercepteur EF Core, piste d'audit HDS, filtre `IPublishable` |
| `Granit.Workflow.Notifications` | Pont approbation → `Granit.Notifications` |
| `Granit.Workflow.Endpoints` | API REST Minimal API (historique, transitions) |
| `@granit/workflow` | Composants React headless (StatusBar, hooks) |

## Architecture

```text
WorkflowDefinition<TState>          ← définition immutable (singleton)
  ↓
WorkflowManager<TState>             ← orchestrateur scoped (permissions + approval routing)
  ↓
WorkflowTransitionInterceptor       ← détection automatique via EF Core SaveChanges
  ↓
WorkflowTransitionRecord            ← piste d'audit HDS immutable (INSERT-only)
```

## Concepts clés

### Définition de workflow (FSM)

Chaque workflow est défini par un enum `TState` et une liste de transitions autorisées.
La définition est **immutable** et conçue pour être un singleton :

```csharp
WorkflowDefinition<DocumentStatus> definition = WorkflowDefinition<DocumentStatus>.Create(b => b
    .InitialState(DocumentStatus.Draft)
    .Transition(DocumentStatus.Draft, DocumentStatus.PendingReview, t => t
        .Named("Soumettre")
        .RequiresPermission("document.submit"))
    .Transition(DocumentStatus.PendingReview, DocumentStatus.Published, t => t
        .Named("Publier")
        .RequiresPermission("document.publish")
        .RequiresApproval())
    .Transition(DocumentStatus.Published, DocumentStatus.Archived, t => t
        .Named("Archiver")
        .RequiresPermission("document.archive")));
```

### Validation du graphe

Le builder valide automatiquement le graphe à `Build()` :

- État initial obligatoire
- Au moins une transition
- Pas de transitions dupliquées
- Tous les états doivent être atteignables depuis l'état initial (BFS)

### Routing d'approbation (style Odoo)

Quand un utilisateur déclenche une transition avec `RequiresApproval = true`
mais **sans** la permission requise :

1. L'état résultant est `PendingReview` (au lieu de l'état cible)
2. Un événement `WorkflowApprovalRequested` est publié
3. `Granit.Workflow.Notifications` notifie les approbateurs désignés
4. Un approbateur (utilisateur avec la permission) peut ensuite valider

#### Résolution des approbateurs Keycloak

Le résolveur intégré `KeycloakApproverResolver` suit le flux :

1. **Permission → rôles** : interroge `IPermissionManager.GetGrantedRolesAsync()`
   pour trouver les rôles ayant la permission requise (via la base de données
   d'autorisation)
2. **Rôles → utilisateurs** : pour chaque rôle, appelle l'API Admin Keycloak
   (`GET /admin/realms/{realm}/roles/{role}/users`) pour obtenir les membres
3. Les identifiants utilisateurs sont dédupliqués et retournés

Configuration requise (`appsettings.json`) :

```json
{
  "KeycloakAdmin": {
    "BaseUrl": "https://keycloak.example.com",
    "Realm": "guava-health",
    "ClientId": "guava-admin-service",
    "ClientSecret": "vault-injected"
  }
}
```

Le service account doit disposer du rôle `realm-management:view-users`.
Le secret doit être injecté depuis Vault — jamais en clair dans la configuration.

### Piste d'audit HDS

Le `WorkflowTransitionInterceptor` crée automatiquement un `WorkflowTransitionRecord`
à chaque changement d'état détecté dans `SaveChanges`. Ce record est **INSERT-only**
(jamais modifié ni supprimé) — conformité HDS 3 ans minimum.

Informations capturées :

- Type et identifiant de l'entité
- État précédent et nouvel état
- Horodatage UTC
- Identifiant de l'utilisateur
- Commentaire optionnel (justification réglementaire)
- Contexte tenant

## Cycle de vie publication

Un cycle de vie pré-construit est fourni via `PublicationWorkflow.Default` :

```text
Draft → PendingReview → Published → Archived
  ↓                                    ↑
  └──── Publication directe ───────────┘
                                Published → Draft (nouvelle version)
```

### Entités versionnées avec workflow

Le versionnement (`BusinessId`, `Version`) est découplé du workflow. Trois cas
d'usage sont supportés :

1. **Versionnement pur** — `IVersioned` seul (voir [persistence.md](../data/persistence.md#versioninginterceptor))
2. **Workflow pur** — `IWorkflowStateful` seul
3. **Versionnement + Workflow** — `VersionedWorkflowEntity`

`VersionedWorkflowEntity` est la classe de base pour le cas 3 (combo) :

- `BusinessId` / `Version` : hérités de `IVersioned`, auto-assignés par `VersioningInterceptor`
- `LifecycleStatus` : état dans le cycle de vie (`WorkflowLifecycleStatus`)
- `IsPublished` : synchronisé automatiquement par le `WorkflowTransitionInterceptor`

> **Note** : l'ancienne classe `VersionedEntity` est un alias obsolète
> (`[Obsolete]`) pour `VersionedWorkflowEntity`. Le code existant continue
> de fonctionner sans modification.

### Filtre `IPublishable`

`IPublishable` est un filtre de requête global EF Core (comme `IActive`, `ISoftDeletable`).
Par défaut, seules les entités avec `IsPublished = true` sont retournées.

Désactivation pour les vues admin :

```csharp
using (dataFilter.Disable<IPublishable>())
{
    // Voir toutes les versions (brouillons inclus)
    List<Document> allVersions = await dbContext.Documents.ToListAsync(ct);
}
```

## Enregistrement DI

```csharp
// Granit.Workflow (core)
services.AddGranitWorkflow();
services.AddWorkflow(PublicationWorkflow.Default);

// Granit.Workflow.EntityFrameworkCore
services.AddGranitWorkflowEntityFrameworkCore();

// Granit.Workflow.Notifications
services.AddGranitWorkflowNotifications();

// Option 1 : résolveur Keycloak intégré (permission → rôle → users via Admin API)
services.AddKeycloakApproverResolver();

// Option 2 : résolveur custom
services.AddWorkflowApproverResolver<MyApproverResolver>();

// Granit.Workflow.Endpoints
services.AddGranitWorkflowEndpoints<AppDbContext>();
app.MapWorkflowEndpoints();
```

## Configuration du DbContext hôte

```csharp
public sealed class AppDbContext : DbContext, IWorkflowDbContext
{
    public DbSet<WorkflowTransitionRecord> WorkflowTransitionRecords => Set<WorkflowTransitionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureWorkflowModule();
    }
}
```

## API REST

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/workflow/{entityType}/{entityId}/history` | Piste d'audit des transitions |

Le préfixe par défaut est `workflow`. Avec `ApiPrefix = "api/v1"`, la route
devient `/api/v1/workflow/{entityType}/{entityId}/history`.

## Composant React

```tsx
import { WorkflowStatusBar, useWorkflowStatus } from "@granit/workflow";

function DocumentHeader({ doc }) {
  const { transitions, isLoading } = useWorkflowStatus("document", doc.id);

  return (
    <WorkflowStatusBar
      entityType="document"
      entityId={doc.id}
      currentState={doc.status}
      states={["Draft", "PendingReview", "Published", "Archived"]}
      transitions={transitions}
      onTransition={(target) => handleTransition(target)}
    />
  );
}
```

## Fichiers associés

- [Définitions](definitions.md)
- [Intercepteur](interceptor.md)
- [Notifications](notifications.md)
