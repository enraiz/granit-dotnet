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

### Dans un module Granit

```csharp
public class GuavaAuthModule : GranitModule
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

**Convention Granit** :

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

`Granit.Observability` enrichit automatiquement chaque log avec :

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
