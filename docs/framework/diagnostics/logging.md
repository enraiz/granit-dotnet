# Logging

Granit n'introduit aucune abstraction de logging. Le logging repose entièrement sur
`Microsoft.Extensions.Logging.ILogger<T>`, le système standard d'ASP.NET Core.
`Granit.Observability` configure Serilog comme implémentation et exporte les logs
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

## `[LoggerMessage]` source-generated — OBLIGATOIRE

Tout le logging dans Granit utilise l'attribut `[LoggerMessage]` (source generator
introduit dans .NET 6). Les appels directs `logger.LogInformation(...)`,
`logger.LogWarning(...)`, etc. sont **interdits** dans le code `src/`.

### Pourquoi

- **Zéro allocation** quand le log level est désactivé (pas de boxing, pas
  d'interpolation, pas de tableau `params object[]`)
- **Structured logging garanti** — les paramètres sont des champs typés, pas du texte
- **Validation compile-time** — erreurs si le message template et les paramètres ne
  correspondent pas

> **Référence Microsoft** :
> [Génération source de journalisation au moment de la compilation](https://learn.microsoft.com/fr-fr/dotnet/core/extensions/logger-message-generator)

### Pattern standard

1. La classe **doit** être `partial`
2. Déclarer des méthodes `private partial void Log...(...)` annotées `[LoggerMessage]`
3. Appeler ces méthodes au lieu des méthodes d'extension `ILogger`

```csharp
internal sealed partial class PatientService(
    ILogger<PatientService> logger,
    AppDbContext db)
{
    public async Task<Guid> CreateAsync(CreatePatientCommand command,
        CancellationToken cancellationToken)
    {
        LogCreatingPatient(command.TenantId);

        // ...

        LogPatientCreated(patient.Id);
        return patient.Id;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Creating patient record for tenant {TenantId}")]
    private partial void LogCreatingPatient(Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Patient record created: {PatientId}")]
    private partial void LogPatientCreated(Guid patientId);
}
```

### Avec exception

Le premier paramètre `Exception` est automatiquement reconnu par le source generator
comme l'exception à attacher au log (pas besoin de `{Exception}` dans le template) :

```csharp
[LoggerMessage(Level = LogLevel.Warning,
    Message = "Failed to get user {UserId} from Keycloak. Returning null")]
private partial void LogGetUserFailed(Exception exception, string userId);
```

### Dans un handler Wolverine

Wolverine supporte l'injection par constructeur primaire. Le handler doit être
`partial` pour utiliser `[LoggerMessage]` :

```csharp
public sealed partial class CreatePatientHandler(
    AppDbContext db,
    ILogger<CreatePatientHandler> logger)
{
    public async Task<PatientCreated> HandleAsync(
        CreatePatient command,
        CancellationToken cancellationToken)
    {
        LogHandlingCreatePatient(command.TenantId);

        // ...

        return new PatientCreated(patientId);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Handling CreatePatient for tenant {TenantId}")]
    private partial void LogHandlingCreatePatient(Guid tenantId);
}
```

### Dans un module Granit

Les modules Granit ne sont pas `partial` par convention. L'initialisation du module
est un cas exceptionnel où un appel direct est toléré (code exécuté une seule fois
au démarrage) :

```csharp
public class MyAppAuthModule : GranitModule
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
            .GetRequiredService<ILogger<MyAppAuthModule>>();

        logger.LogInformation("MyAppAuthModule initialized");
    }
}
```

## Niveaux de log

| Niveau | Méthode | Usage |
| --- | --- | --- |
| `Trace` | `LogTrace` | Diagnostic très verbeux (désactivé en production) |
| `Debug` | `LogDebug` | Diagnostic de développement |
| `Information` | `LogInformation` | Événements métier normaux |
| `Warning` | `LogWarning` | Situations anormales récupérables |
| `Error` | `LogError` | Erreurs impactant une opération |
| `Critical` | `LogCritical` | Défaillances systémiques |

**Convention Granit** — tous ces exemples utilisent `[LoggerMessage]` :

```csharp
// Information — événements métier normaux
[LoggerMessage(Level = LogLevel.Information,
    Message = "Patient {PatientId} discharged from {Ward}")]
private partial void LogPatientDischarged(Guid patientId, string ward);

// Warning — situation inattendue mais non bloquante
[LoggerMessage(Level = LogLevel.Warning,
    Message = "Tenant {TenantId} not found, using default configuration")]
private partial void LogTenantNotFound(Guid tenantId);

// Error — opération échouée, avec exception
[LoggerMessage(Level = LogLevel.Error,
    Message = "Failed to synchronize resource {ResourceId}")]
private partial void LogResourceSyncFailed(Exception exception, string resourceId);

// Critical — service indisponible, intervention requise
[LoggerMessage(Level = LogLevel.Critical,
    Message = "Vault connection lost — credential renewal will fail")]
private partial void LogVaultConnectionLost();
```

## Logs structurés (message templates)

Serilog utilise des **message templates** avec des paramètres nommés entre accolades.
Ces paramètres sont indexés comme propriétés cherchables dans Loki. Avec
`[LoggerMessage]`, les paramètres sont des arguments typés de la méthode :

```csharp
// ✅ Correct — [LoggerMessage] avec paramètres typés
[LoggerMessage(Level = LogLevel.Information,
    Message = "Consent recorded: patient={PatientId} type={ConsentType}")]
private partial void LogConsentRecorded(Guid patientId, string consentType);

// ❌ Interdit — appel direct sans source generator
_logger.LogInformation("Consent recorded: patient={PatientId} type={ConsentType}",
    patientId, consentType);

// ❌ Interdit — interpolation de chaînes, perd la structure
_logger.LogInformation($"Consent recorded: patient={patientId} type={consentType}");
```

Les propriétés apparaissent dans Loki comme labels ou champs JSON :

```json
{
  "Timestamp": "2025-11-15T10:30:45Z",
  "Level": "Information",
  "MessageTemplate": "Consent recorded: patient={PatientId} type={ConsentType}",
  "PatientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ConsentType": "DATA_SHARE",
  "SourceContext": "MyApp.Modules.Auth.Handlers.RecordConsentHandler",
  "TraceId": "abc123def456",
  "SpanId": "789xyz"
}
```

## Enrichissement automatique

`Granit.Observability` enrichit automatiquement chaque log avec :

| Propriété | Source | Description |
| --- | --- | --- |
| `ServiceName` | `ObservabilityOptions.ServiceName` | Nom du service (ex: `"my-backend"`) |
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
avec `TenantId` quand un tenant est résolu (voir [multi-tenancy.md](../data/multi-tenancy.md)).

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
    }
  }
}
```

**Par environnement** (`appsettings.Development.json`) :

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

## Règles ISO 27001 et RGPD

### Ce qui est INTERDIT dans les logs

Ne jamais inclure ces données dans un message `[LoggerMessage]` :

- Données patient (PII / données de santé) : nom, diagnostic, numéro de sécurité
  sociale
- Données personnelles identifiantes : email, nom, prénom
- Secrets, tokens, credentials, connection strings
- Clés de chiffrement

### Ce qui est autorisé

```csharp
// ✅ Identifiants pseudonymisés (GUID non rattachable sans accès à la base)
[LoggerMessage(Level = LogLevel.Information,
    Message = "Patient record updated: {PatientId}")]
private partial void LogPatientUpdated(Guid patientId);

// ✅ Métriques techniques sans donnée personnelle
[LoggerMessage(Level = LogLevel.Information,
    Message = "Resource synchronized: type={ResourceType} count={Count}")]
private partial void LogResourceSynced(string resourceType, int count);

// ✅ Erreurs avec code, sans donnée personnelle
[LoggerMessage(Level = LogLevel.Error,
    Message = "Resource {ResourceId} failed validation: {ErrorCode}")]
private partial void LogResourceValidationFailed(
    Exception exception, string resourceId, string errorCode);
```

**Règle** : les logs peuvent contenir des **identifiants pseudonymisés** (GUID, identifiants
techniques) mais jamais des données directement identifiantes (nom, prénom, email, numéro
de sécurité sociale, diagnostic, etc.).

> **ISO 27001** : les traces d'audit contenant des données de santé transitent par
> les intercepteurs EF Core (voir [persistence.md](../data/persistence.md)), pas par les logs
> applicatifs. Les logs Serilog sont des logs **techniques**, pas des logs d'audit.

## Corrélation avec les traces OpenTelemetry

Chaque requête HTTP génère automatiquement un `TraceId` et un `SpanId` via
`Granit.Observability`. Ces identifiants sont injectés dans chaque log par Serilog,
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

1. **`[LoggerMessage]` obligatoire** — toujours utiliser le source generator, jamais
   d'appels directs `logger.LogInformation(...)` dans `src/`. La seule exception
   tolérée est le code d'initialisation de module (`OnApplicationInitialization`)
2. **Classe `partial`** — toute classe qui émet des logs doit être déclarée `partial`
3. **`ILogger<T>` uniquement** — ne jamais appeler Serilog directement
   (`Log.Information(...)`) dans le code applicatif
4. **Message templates** — toujours utiliser des paramètres nommés `{Property}`,
   jamais l'interpolation `$"..."`
5. **Pas de PII** — aucune donnée personnelle ou de santé dans les logs (RGPD + ISO 27001)
6. **Pas de secrets** — aucun token, mot de passe, clé dans les logs
7. **Identifiants techniques** — utiliser les GUID des entités comme identifiants
   dans les logs (pseudonymisation)
8. **Niveau adapté** — `Information` pour les événements métier, `Warning` pour
   les anomalies, `Error` avec exception pour les échecs
9. **`NullLogger` dans les tests** — évite la configuration Serilog dans les tests
   unitaires ; NSubstitute pour vérifier les appels de log si nécessaire
