# Implémentation (.NET / C#)

[← Conventions](../index.md)

## Gestion du temps

**Interdiction formelle** d'utiliser directement :

- `DateTime.Now`, `DateTime.UtcNow`
- `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`

Injectez toujours `TimeProvider` (abstraction native .NET 8+) ou `IClock`
(Granit.Timing) pour obtenir l'heure courante.

```csharp
// ✅ via IClock (Granit.Timing)
DateTimeOffset now = clock.Now;

// ❌ appel statique non-testable
DateTimeOffset now = DateTimeOffset.UtcNow;
```

Autres règles :

- Préférez `DateTimeOffset` (précision timezone) à `DateTime`
- Utilisez `DateOnly` / `TimeOnly` quand la composante date ou heure seule suffit
- En tests : `FakeTimeProvider` (Microsoft.Extensions.TimeProvider.Testing) pour
  des assertions déterministes

## Async / await

- **Suffixe `Async`** obligatoire sur toutes les méthodes asynchrones
- **`CancellationToken`** en dernier paramètre avec `= default`

  ```csharp
  public async Task<string> DecryptAsync(
      string keyName,
      string ciphertext,
      CancellationToken cancellationToken = default)
  ```

- **`ConfigureAwait(false)`** dans le code bibliothèque (packages NuGet) — le
  consommateur peut avoir un `SynchronizationContext`

  ```csharp
  Secret<DecryptionResponse> result = await _vaultClient.V1.Secrets.Transit
      .DecryptAsync(keyName, requestOptions, mountPoint: _options.TransitMountPoint)
      .ConfigureAwait(false);
  ```

- **`.WaitAsync(cancellationToken)`** pour les API sans support natif du
  `CancellationToken` (ex. VaultSharp)
- **`Task.CompletedTask`** pour les méthodes void async sans opération

## Nullabilité et validation des paramètres

### Nullable Reference Types (NRT)

NRT est activé globalement (`<Nullable>enable</Nullable>` dans `Directory.Build.props`).

- `?` pour les types nullable : `string?`, `Guid?`
- Vérification avec `is null` / `is not null` (pattern matching)
- Propriétés `string` non-nullable initialisées à `string.Empty`

### Guard clauses

Préférez les méthodes statiques du framework pour la validation des paramètres :

```csharp
// ✅ moderne — 1 ligne, nom du paramètre déduit automatiquement
ArgumentNullException.ThrowIfNull(service);
ArgumentException.ThrowIfNullOrEmpty(name);

// ❌ verbeux — à réserver quand null est un cas normal (pas une erreur)
if (service is null)
{
    throw new ArgumentNullException(nameof(service));
}
```

Utilisez `is null` + early return uniquement quand `null` est un cas de flux
normal (pas une condition d'erreur).

## Collections et types de retour

- **Types de retour** : préférez les interfaces en lecture seule (`IReadOnlyList<T>`,
  `IReadOnlyCollection<T>`) pour les collections retournées
- `IEnumerable<T>` uniquement pour l'évaluation paresseuse (streaming)
- **Ne jamais retourner `IQueryable<T>` hors de la couche Persistence** —
  c'est une leaky abstraction qui expose les détails d'implémentation EF Core
- Expressions de collection C# 12+ pour l'initialisation :

  ```csharp
  List<string> empty = [];
  List<Type> types = [.. _modules.Select(m => m.ModuleType)];
  ```

## Records vs classes

| Type | Usage | Exemple |
| --- | --- | --- |
| `sealed record` | Objets de données immuables, DTOs légers | `sealed record EmbeddedJsonSource(Assembly Assembly, string ResourcePrefix)` |
| `class` | Entités de domaine, services, intercepteurs | `sealed class AuditedEntityInterceptor` |

Conventions complémentaires :

- `record` avec `required` + `init` pour les structures plus complexes
- `init` pour les propriétés qui ne doivent pas changer après construction
  (DTOs non bindés)
- `set` pour les classes d'options (nécessaire pour `BindConfiguration`)

## Logging structuré

Utilisez **`[LoggerMessage]` source-generated** — jamais d'interpolation de chaîne
dans les appels de log.

```csharp
[LoggerMessage(
    Level = LogLevel.Debug,
    Message = "Data encrypted with Transit key {KeyName}")]
private static partial void LogEncrypted(ILogger logger, string keyName);
```

Avantages :

- Zéro allocation si le niveau de log n'est pas actif
- AOT-compatible (NativeAOT)
- Placeholders typés et vérifiés à la compilation

## Regex

Utilisez **`[GeneratedRegex]`** (source-generated) — jamais `new Regex(...,
RegexOptions.Compiled)`.

```csharp
[GeneratedRegex(@"^[a-z0-9-]+$", RegexOptions.None, 100)]
private static partial Regex SlugRegex();
```

- Le pattern est compilé au build (zéro coût runtime, NativeAOT-compatible)
- **Timeout obligatoire** (3e paramètre, en ms) pour les regex sur input utilisateur
- Ne jamais combiner `RegexOptions.Compiled` avec `[GeneratedRegex]` — il est ignoré
  par le source generator

## Gestion des exceptions

- **Exceptions métier** : `BusinessException` avec `IHasErrorCode`,
  `IUserFriendlyException`
- **Guard clauses** en début de méthode (early return, pas de nesting profond)
- **Pas de `catch` vide** — toujours logger ou relancer
- `ArgumentNullException.ThrowIfNull()` pour les paramètres non-nullable
- Utilisez `throw;` (pas `throw ex;`) pour préserver la stack trace

## Gestion des ressources

- Implémentez `IAsyncDisposable` (préféré) ou `IDisposable` pour les classes qui
  détiennent des ressources
- Pas de finaliseur — utilisez le pattern Dispose moderne
- Utilisez `await using` pour les ressources async :

  ```csharp
  await using ChromiumLifetimeService service = new(options);
  ```

## Observabilité (Metrics et Tracing)

Granit utilise OpenTelemetry pour l'instrumentation. Chaque package qui effectue
des opérations significatives doit exposer ses propres sources.

### Déclaration des sources

Déclarez `ActivitySource` et `Meter` comme champs **statiques** au niveau de la
classe, avec le nom du package comme identifiant :

```csharp
private static readonly ActivitySource ActivitySource = new("Granit.Vault");
private static readonly Meter Meter = new("Granit.Vault");
private static readonly Counter<long> EncryptionCounter =
    Meter.CreateCounter<long>("granit.vault.encryption_requests");
```

### Tracing — `Activity`

```csharp
public async Task<string> EncryptAsync(string keyName, string plaintext,
    CancellationToken cancellationToken = default)
{
    using Activity? activity = ActivitySource.StartActivity("EncryptData");
    activity?.SetTag("vault.key_name", keyName);

    // ... logic

    EncryptionCounter.Add(1);
    return ciphertext;
}
```

Bonnes pratiques :

- `using` sur `Activity` pour garantir la fin du span
- `?.` car `StartActivity` retourne `null` si aucun listener n'est enregistré
- `SetTag` pour les attributs métier (pas de PII)

### Metrics — `Meter`, `Counter`, `Histogram`

| Type | Usage | Exemple |
| --- | --- | --- |
| `Counter<long>` | Nombre d'occurrences | Requêtes traitées, erreurs |
| `Histogram<double>` | Distribution de valeurs | Latence, taille de payload |
| `UpDownCounter<long>` | Valeur qui monte et descend | Connexions actives |

Conventions de nommage des métriques :

- Préfixe par le nom du package : `granit.vault.*`, `granit.persistence.*`
- Snake\_case avec points : `granit.vault.encryption_requests`
- Unité dans le nom si pertinent : `granit.vault.encryption_duration_ms`

### Enregistrement

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder.AddSource("Granit.Vault"))
    .WithMetrics(builder => builder.AddMeter("Granit.Vault"));
```

> Voir aussi : [référence observabilité](../../framework/observability/index.md)
