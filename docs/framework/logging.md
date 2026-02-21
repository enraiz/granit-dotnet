# Logging

Les packages Foundation utilisent le système de logging intégré d'ASP.NET Core
(`Microsoft.Extensions.Logging`). Aucune dépendance supplémentaire n'est requise
pour émettre des logs depuis les modules Foundation.

Référence officielle : [Logging in .NET and ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0)

## Injection du logger

Chaque classe reçoit un `ILogger<T>` via injection de dépendances. La catégorie
de log correspond au nom complet du type.

```csharp
public sealed class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
}
```

## Niveaux de log

| Niveau | Méthode | Usage |
| --- | --- | --- |
| `Trace` | `LogTrace` | Détails très fins, potentiellement sensibles. Désactivé par défaut. |
| `Debug` | `LogDebug` | Informations de débogage. Volume élevé, à utiliser avec prudence en production. |
| `Information` | `LogInformation` | Flux normal de l'application (démarrage, événements métier). |
| `Warning` | `LogWarning` | Situations anormales qui ne causent pas d'erreur (retry, fallback). |
| `Error` | `LogError` | Erreurs dans l'opération courante (pas une panne globale). |
| `Critical` | `LogCritical` | Pannes nécessitant une intervention immédiate (perte de données, disque plein). |

### Choix du niveau

- **Production** : minimum `Information` pour le code applicatif, `Warning` pour
  les catégories framework (`Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`).
- **Développement** : `Debug` pour les catégories en cours d'investigation uniquement.
- **JAMAIS** de `Trace` en production sauf investigation ponctuelle ciblée.

## Logging structuré

Toujours utiliser des message templates avec des paramètres nommés.

```csharp
// Correct : structured logging
_logger.LogInformation("Credentials obtenus pour {Username}, TTL={TTL}s", username, ttl);

// Incorrect : interpolation de chaîne
_logger.LogInformation($"Credentials obtenus pour {username}, TTL={ttl}s");
```

L'interpolation de chaîne évalue et alloue la chaîne **même si le niveau de log
est désactivé**. Les message templates permettent aux providers (Serilog, OTLP)
d'indexer les propriétés structurées (`Username`, `TTL`) pour le filtrage et la
recherche.

## LoggerMessage source generators (haute performance)

Pour le code appelé fréquemment ou les chemins sensibles à la performance,
utiliser l'attribut `[LoggerMessage]` qui génère du code optimisé à la compilation.

L'analyseur `CA1873` signale les appels `ILogger.LogXxx()` dont les arguments
sont évalués inutilement quand le log level est désactivé. La solution est
de passer aux source generators.

### Avant (déclenche CA1873)

```csharp
_logger.LogDebug("Renouvellement du lease {LeaseId}", _leaseId);
```

### Après (source generator)

```csharp
public sealed partial class VaultCredentialLeaseManager
{
    // ...

    LogLeaseRenewing(_logger, _leaseId);

    // ...

    [LoggerMessage(Level = LogLevel.Debug, Message = "Renouvellement du lease {LeaseId}")]
    private static partial void LogLeaseRenewing(ILogger logger, string leaseId);
}
```

### Règles d'utilisation

1. La classe **doit** être `partial`
2. La méthode de log est `private static partial void`
3. Le premier paramètre est toujours `ILogger logger`
4. Les paramètres suivants correspondent aux placeholders `{...}` du message
5. Pour logger une exception, ajouter un paramètre `Exception` (nommé `ex` par convention)

```csharp
[LoggerMessage(Level = LogLevel.Warning, Message = "Échec du renouvellement du lease {LeaseId}")]
private static partial void LogLeaseRenewalFailed(ILogger logger, string leaseId, Exception ex);
```

### Quand utiliser les source generators

| Situation | Approche |
| --- | --- |
| Code appelé à haute fréquence (boucles, middleware) | `[LoggerMessage]` obligatoire |
| Services infrastructure (Vault, Persistence) | `[LoggerMessage]` recommandé |
| Handlers métier appelés ponctuellement | `ILogger.LogXxx()` acceptable |
| Code de démarrage (exécuté une seule fois) | `ILogger.LogXxx()` acceptable |

En cas de doute, préférer `[LoggerMessage]` — l'analyseur CA1873 signalera
les cas problématiques.

## Sécurité et conformité

### Données interdites dans les logs

Conformité HDS et RGPD — ne JAMAIS logger :

- Mots de passe, tokens, clés API, secrets Vault
- Données de santé (FHIR, diagnostics, prescriptions)
- Données personnelles identifiantes (nom complet, email, numéro patient)
- Adresses IP complètes (pseudonymiser si nécessaire pour le diagnostic)

```csharp
// Interdit
_logger.LogInformation("Connexion avec token {Token}", vaultToken);

// Correct : ne logger que l'identifiant non-sensible
_logger.LogInformation("Connexion Vault établie, méthode={AuthMethod}", authMethod);
```

### Audit trail

Les opérations métier critiques (création, modification, suppression d'entités)
sont tracées via `AuditableEntity` et `AuditLogEntry` (package Core), pas via
les logs applicatifs. Les logs complètent l'audit trail pour le diagnostic
opérationnel.

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Configuration par environnement

`appsettings.Development.json` peut abaisser le niveau pour le débogage local :

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "DigitalDynamics": "Debug"
    }
  }
}
```

### Catégories utiles

| Catégorie | Contenu |
| --- | --- |
| `DigitalDynamics.Foundation.Vault` | Opérations Vault (credentials, transit, leases) |
| `DigitalDynamics.Foundation.Persistence` | Interceptors EF Core (audit, soft delete) |
| `DigitalDynamics.Foundation.Observability` | Configuration OTEL et Serilog |
| `Microsoft.AspNetCore.Authentication` | Authentification OIDC/JWT |
| `Microsoft.EntityFrameworkCore.Database.Command` | Requêtes SQL exécutées |

## Scopes de log

Les scopes regroupent des logs liés à une opération logique (transaction,
requête HTTP). Utiliser `ILogger.BeginScope()` pour ajouter du contexte :

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["OperationId"] = operationId,
    ["EntityType"] = typeof(Patient).Name
}))
{
    _logger.LogInformation("Début du traitement");
    // Tous les logs dans ce bloc incluent OperationId et EntityType
}
```

Les scopes sont automatiquement enrichis par OpenTelemetry avec `TraceId`,
`SpanId` et `ParentId` pour la corrélation distribuée
(voir [observability.md](observability.md)).

## Intégration avec la stack d'observabilité

Foundation.Observability configure automatiquement Serilog comme provider de
logging et exporte les logs via OTLP vers Loki. La documentation complète
de la stack d'observabilité (Serilog, OpenTelemetry, OTLP) est dans
[observability.md](observability.md).

```text
ILogger → Serilog → OTLP gRPC → OpenTelemetry Collector → Loki
```

## Anti-patterns

### Interpolation de chaîne

```csharp
// Mauvais : allocation même si Debug est désactivé
_logger.LogDebug($"Traitement de {items.Count} éléments en {sw.Elapsed}");

// Bon : message template
_logger.LogDebug("Traitement de {Count} éléments en {Elapsed}", items.Count, sw.Elapsed);
```

### Logger et relancer une exception

```csharp
// Mauvais : l'exception est loggée deux fois (ici + middleware global)
catch (Exception ex)
{
    _logger.LogError(ex, "Erreur");
    throw;
}

// Bon : laisser le middleware global gérer, ou logger uniquement si on gère l'erreur
catch (HttpRequestException ex)
{
    _logger.LogWarning(ex, "Appel externe échoué, retry dans {Delay}s", retryDelay);
    await Task.Delay(TimeSpan.FromSeconds(retryDelay));
}
```

### Surcharge de logs en production

```csharp
// Mauvais : log dans une boucle à haute fréquence
foreach (var item in items)
{
    _logger.LogInformation("Traitement de {ItemId}", item.Id);
}

// Bon : log un résumé
_logger.LogInformation("Traitement de {Count} éléments terminé", items.Count);
```

### Concaténation de l'exception dans le message

```csharp
// Mauvais : l'exception est sérialisée dans le message
_logger.LogError("Erreur : " + ex.ToString());

// Bon : passer l'exception en premier paramètre
_logger.LogError(ex, "Erreur lors du traitement de {EntityId}", entityId);
```
