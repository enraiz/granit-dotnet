# Logging

<<<<<<< HEAD
Foundation n'introduit aucune abstraction de logging. Le logging repose entièrement sur
`Microsoft.Extensions.Logging.ILogger<T>`, le système standard d'ASP.NET Core.
`Foundation.Observability` configure Serilog comme implémentation et exporte les logs
vers Loki via OTLP.

> **Référence Microsoft** :
> [Logging dans .NET](https://learn.microsoft.com/fr-fr/dotnet/core/extensions/logging)
>
> **Voir aussi** : [observability.md](observability.md) pour la configuration Serilog
> et l'export OTLP.

## Principe

```text
Code applicatif          Abstraction Microsoft          Implémentation Serilog
────────────────         ────────────────────           ──────────────────────
ILogger<MyService>  →   Microsoft.Extensions.Logging  →  Serilog → OTLP → Loki
```

Le code applicatif n'a jamais de dépendance directe sur Serilog — uniquement sur
`ILogger<T>`. L'implémentation peut être remplacée sans modifier le code métier.

## Utilisation

### Dans un service (injection par constructeur)

```csharp
public class PatientService
{
    private readonly ILogger<PatientService> _logger;
    private readonly AppDbContext _db;

    public PatientService(ILogger<PatientService> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<Guid> CreateAsync(CreatePatientCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating patient record for tenant {TenantId}",
            command.TenantId);

        // ...

        _logger.LogInformation("Patient record created: {PatientId}", patient.Id);
        return patient.Id;
    }
}
```

### Dans un handler Wolverine (method injection)

Wolverine supporte l'injection de dépendances directement dans les paramètres de
méthode `Handle` :

```csharp
public static async Task<PatientCreated> Handle(
    CreatePatient command,
    AppDbContext db,
    ILogger<CreatePatient> logger,
    CancellationToken cancellationToken)
{
    logger.LogInformation("Handling CreatePatient for tenant {TenantId}",
        command.TenantId);

    // ...

    return new PatientCreated(patientId);
}
```

### Dans un module Foundation

```csharp
public class GuavaAuthModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // ILogger<T> non disponible ici (DI pas encore construit)
        // Utiliser les logs ASP.NET Core host builder à la place
    }

    public override void OnApplicationInitialization(
        ApplicationInitializationContext context)
    {
        var logger = context.ServiceProvider
            .GetRequiredService<ILogger<GuavaAuthModule>>();

        logger.LogInformation("GuavaAuthModule initialized");
=======
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
>>>>>>> feature/settings-module
    }
}
```

## Niveaux de log

| Niveau | Méthode | Usage |
| --- | --- | --- |
<<<<<<< HEAD
| `Trace` | `LogTrace` | Diagnostic très verbeux (désactivé en production) |
| `Debug` | `LogDebug` | Diagnostic de développement |
| `Information` | `LogInformation` | Événements métier normaux |
| `Warning` | `LogWarning` | Situations anormales récupérables |
| `Error` | `LogError` | Erreurs impactant une opération |
| `Critical` | `LogCritical` | Défaillances systémiques |

**Convention Foundation** :

```csharp
// Information — événements métier normaux
_logger.LogInformation("Patient {PatientId} discharged from {Ward}", id, ward);

// Warning — situation inattendue mais non bloquante
_logger.LogWarning("Tenant {TenantId} not found, using default configuration", tenantId);

// Error — opération échouée, avec exception
_logger.LogError(ex, "Failed to synchronize FHIR resource {ResourceId}", resourceId);

// Critical — service indisponible, intervention requise
_logger.LogCritical("Vault connection lost — credential renewal will fail");
```

## Logs structurés (message templates)

Serilog utilise des **message templates** avec des paramètres nommés entre accolades.
Ces paramètres sont indexés comme propriétés cherchables dans Loki — ne pas utiliser
l'interpolation de chaînes `$"..."`.

```csharp
// ✅ Correct — propriétés indexées dans Loki
_logger.LogInformation("Consent recorded: patient={PatientId} type={ConsentType}",
    patientId, consentType);

// ❌ Incorrect — perd la structure, non cherchable
_logger.LogInformation($"Consent recorded: patient={patientId} type={consentType}");
```

Les propriétés apparaissent dans Loki comme labels ou champs JSON :

```json
{
  "Timestamp": "2025-11-15T10:30:45Z",
  "Level": "Information",
  "MessageTemplate": "Consent recorded: patient={PatientId} type={ConsentType}",
  "PatientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ConsentType": "FHIR_SHARE",
  "SourceContext": "Guava.Modules.Auth.Handlers.RecordConsentHandler",
  "TraceId": "abc123def456",
  "SpanId": "789xyz"
}
```

## Enrichissement automatique

`Foundation.Observability` enrichit automatiquement chaque log avec :

| Propriété | Source | Description |
| --- | --- | --- |
| `ServiceName` | `ObservabilityOptions.ServiceName` | Nom du service (ex: `"guava-backend"`) |
| `ServiceVersion` | `ObservabilityOptions.ServiceVersion` | Version du service |
| `Environment` | `ObservabilityOptions.Environment` | Environnement de déploiement |
| `TraceId` | OpenTelemetry | Identifiant de trace distribué (corrélation) |
| `SpanId` | OpenTelemetry | Identifiant du span courant |
| `SourceContext` | Serilog | Nom complet du type `T` dans `ILogger<T>` |

La présence de `TraceId` et `SpanId` permet de corréler un log avec la trace
correspondante dans Grafana (clic sur un log → span Tempo associé).

## Enrichissement contextuel avec LogContext

Pour ajouter des propriétés à tous les logs d'un bloc de code :

```csharp
using (LogContext.PushProperty("TenantId", tenantId))
using (LogContext.PushProperty("RequestId", requestId))
{
    _logger.LogInformation("Processing request");
    await ProcessAsync();
    _logger.LogInformation("Request completed");
    // Les deux logs contiennent TenantId et RequestId
}
```

Le middleware `TenantResolutionMiddleware` enrichit automatiquement le contexte
avec `TenantId` quand un tenant est résolu (voir [multi-tenancy.md](multi-tenancy.md)).

## Configuration des niveaux

Les niveaux minimum sont configurés dans `appsettings.json` via la section `Serilog` :

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.AspNetCore.Hosting.Diagnostics": "Information",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "Wolverine": "Warning",
        "System": "Warning"
      }
=======
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
sont tracées via la hiérarchie `AuditedEntity` et `AuditLogEntry` (package Core), pas via
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
>>>>>>> feature/settings-module
    }
  }
}
```

<<<<<<< HEAD
**Par environnement** (`appsettings.Development.json`) :

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
=======
### Configuration par environnement

`appsettings.Development.json` peut abaisser le niveau pour le débogage local :

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "DigitalDynamics": "Debug"
>>>>>>> feature/settings-module
    }
  }
}
```

<<<<<<< HEAD
## Règles HDS et RGPD

### Ce qui est INTERDIT dans les logs

```csharp
// ❌ INTERDIT — données patient (PII / données de santé)
_logger.LogInformation("Patient {Name} diagnosed with {Diagnosis}", patient.Name, diagnosis);

// ❌ INTERDIT — données personnelles identifiantes
_logger.LogInformation("User email: {Email}", user.Email);

// ❌ INTERDIT — secrets, tokens, credentials
_logger.LogDebug("Vault token: {Token}", vaultToken);
_logger.LogDebug("Connection string: {ConnectionString}", connectionString);

// ❌ INTERDIT — clés de chiffrement
_logger.LogDebug("Encryption key: {Key}", Convert.ToBase64String(key));
```

### Ce qui est autorisé

```csharp
// ✅ Identifiants pseudonymisés (GUID non rattachable sans accès à la base)
_logger.LogInformation("Patient record updated: {PatientId}", patient.Id);

// ✅ Métriques techniques sans donnée personnelle
_logger.LogInformation("FHIR resource synchronized: type={ResourceType} count={Count}",
    resourceType, count);

// ✅ Erreurs avec code, sans donnée personnelle
_logger.LogError(ex, "FHIR resource {ResourceId} failed validation: {ErrorCode}",
    resourceId, errorCode);
```

**Règle** : les logs peuvent contenir des **identifiants pseudonymisés** (GUID, identifiants
techniques) mais jamais des données directement identifiantes (nom, prénom, email, numéro
de sécurité sociale, diagnostic, etc.).

> **HDS EXI-04** : les traces d'audit contenant des données de santé transitent par
> les intercepteurs EF Core (voir [persistence.md](persistence.md)), pas par les logs
> applicatifs. Les logs Serilog sont des logs **techniques**, pas des logs d'audit.

## Corrélation avec les traces OpenTelemetry

Chaque requête HTTP génère automatiquement un `TraceId` et un `SpanId` via
`Foundation.Observability`. Ces identifiants sont injectés dans chaque log par Serilog,
permettant la corrélation dans Grafana :

```text
Requête HTTP → TraceId: abc123
  ├─ Log: "Creating patient record" [TraceId: abc123, SpanId: def456]
  ├─ Span EF Core: INSERT INTO patients [TraceId: abc123]
  └─ Log: "Patient record created" [TraceId: abc123, SpanId: def456]
```

Dans Grafana : cliquer sur un log → affiche le span Tempo correspondant avec la
durée et les attributs complets de l'opération.

## Tests

Pour les tests unitaires, utiliser `ILogger<T>` avec un logger de substitution ou
un logger de test :

```csharp
// Option 1 : NSubstitute (vérifier les appels)
var logger = Substitute.For<ILogger<PatientService>>();
var service = new PatientService(logger, db);

await service.CreateAsync(command, CancellationToken.None);

logger.Received().Log(
    LogLevel.Information,
    Arg.Any<EventId>(),
    Arg.Is<object>(o => o.ToString()!.Contains("Patient record created")),
    null,
    Arg.Any<Func<object, Exception?, string>>());

// Option 2 : Microsoft.Extensions.Logging.Abstractions (no-op, plus simple)
var logger = NullLogger<PatientService>.Instance;
var service = new PatientService(logger, db);
```

## Bonnes pratiques

1. **Toujours utiliser `ILogger<T>`** — ne jamais appeler Serilog directement
   (`Log.Information(...)`) dans le code applicatif
2. **Message templates** — toujours utiliser des paramètres nommés `{Property}`,
   jamais l'interpolation `$"..."`
3. **Pas de PII** — aucune donnée personnelle ou de santé dans les logs (RGPD + HDS)
4. **Pas de secrets** — aucun token, mot de passe, clé dans les logs
5. **Identifiants techniques** — utiliser les GUID des entités comme identifiants
   dans les logs (pseudonymisation)
6. **Niveau adapté** — `Information` pour les événements métier, `Warning` pour
   les anomalies, `Error` avec exception pour les échecs
7. **`NullLogger` dans les tests** — évite la configuration Serilog dans les tests
   unitaires ; NSubstitute pour vérifier les appels de log si nécessaire
=======
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
>>>>>>> feature/settings-module
