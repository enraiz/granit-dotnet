# Idempotency

`Granit.Idempotency` implémente l'idempotence HTTP style Stripe
pour les API publiques Digital Dynamics. Un client peut renvoyer la même requête
(panne réseau, timeout, double-clic) et obtenir la réponse originale sans
ré-exécution de la logique métier.

> **Conformité HDS** : les entrées d'idempotence stockées dans Redis contiennent
> potentiellement des données de santé (corps de réponse). Elles sont chiffrées
> avec `ICacheValueEncryptor` (AES-256-CBC) via le module
> `Granit.Caching`.

## Machine à états

```text
Absent ──(TryAcquire SET NX PX)──► InProgress ──(SetCompleted SET XX PX)──► Completed
                                        │
                                        └──(5xx / timeout / exception)──► Absent
```

| État | Stockage Redis | TTL |
| --- | --- | --- |
| `InProgress` | Lock atomique (SET NX PX) | `InProgressTtl` (30 s) |
| `Completed` | Réponse complète chiffrée | `CompletedTtl` (24 h) |

## Installation

```bash
dotnet add package Granit.Idempotency
```

## Configuration rapide

```csharp
[DependsOn(typeof(GranitIdempotencyModule))]
public sealed class AppModule : GranitModule { }
```

Le module appelle automatiquement `AddGranitIdempotency` en lisant la section
`Idempotency` de `appsettings.json`.

Enregistrer le middleware dans le pipeline ASP.NET Core **après**
authentication/authorization :

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseGranitIdempotency(); // après auth — ICurrentUserService doit être peuplé
app.MapControllers();
```

## appsettings.json

```json
{
  "Idempotency": {
    "HeaderName": "Idempotency-Key",
    "KeyPrefix": "idp",
    "CompletedTtl": "24:00:00",
    "InProgressTtl": "00:00:30",
    "ExecutionTimeout": "00:00:25",
    "MaxBodySizeBytes": 1048576
  }
}
```

## Utilisation

### Décorer un endpoint

```csharp
[HttpPost("payments")]
[Idempotent]
public async Task<IActionResult> CreatePaymentAsync(
    [FromBody] CreatePaymentRequest request,
    CancellationToken ct)
{
    // Exécuté une seule fois, même si le client renvoie la requête.
    Payment payment = await _paymentService.CreateAsync(request, ct);
    return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
}
```

Le client envoie le header `Idempotency-Key` avec une valeur unique (UUID v4
recommandé) :

```http
POST /api/v1/payments HTTP/1.1
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{"amount": 100, "currency": "EUR"}
```

### Minimal APIs

```csharp
app.MapPost("/payments", CreatePaymentAsync)
   .WithMetadata(new IdempotentAttribute())
   .WithName("CreatePayment");
```

### Comportement selon le scénario

| Scénario | Réponse | Header |
| --- | --- | --- |
| Première requête (lock acquis) | Réponse métier | — |
| Retry — réponse déjà stockée (hash OK) | Réponse originale rejouée | `X-Idempotency-Replayed: true` |
| Double-clic — exécution en cours | `409 Conflict` | `Retry-After: 30` |
| Retry avec body différent | `422 Unprocessable Entity` | — |
| Timeout d'exécution | `503 Service Unavailable` | — |
| Réponse 5xx | Lock libéré, client peut réessayer | — |

## Hachage du payload

La clé Redis composite est :

```text
{KeyPrefix}:{tenantId}:{userId}:{METHOD}:{routePattern}:{sha256(idempotencyKey)}
```

Le **hash de validation** couvre : `METHOD + routePattern + idempotencyKeyValue + body`
(SHA-256, streaming via `IncrementalHash` + `ArrayPool<byte>`, cap à `MaxBodySizeBytes`).

Si le client renvoie la même `Idempotency-Key` avec un body différent, le middleware
retourne `422 Unprocessable Entity` — la clé est liée à la requête originale.

> **Multipart exclus** : `multipart/form-data` est rejeté avec `422` car les
> boundaries varient à chaque requête, rendant le hachage non déterministe.

## Codes HTTP mis en cache

| Codes | Comportement |
| --- | --- |
| `2xx`, `400`, `404`, `409`, `410`, `422` | Stockés et rejoués |
| `401`, `403` | Jamais mis en cache (les droits peuvent changer) |
| `5xx` | Lock libéré (`DeleteAsync`) — client peut réessayer |
| Timeout (`503`) | Lock libéré — client peut réessayer après `InProgressTtl` |
| `499` Client disconnect | Lock expire naturellement (jamais de cache partiel) |

## TTL et garanties de concurrence

`ExecutionTimeout` doit être strictement inférieur à `InProgressTtl` :

```text
ExecutionTimeout (25 s) < InProgressTtl (30 s)
```

Ce gap garantit que le handler métier s'arrête **avant** que le lock Redis expire.
Sans ce gap, un lock expiré laisserait un autre pod traiter la même requête
simultanément.

La validation est appliquée au démarrage via `IValidateOptions<IdempotencyOptions>`.

## Options de configuration

### `IdempotencyOptions` (section `Idempotency`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `HeaderName` | `string` | `"Idempotency-Key"` | Nom du header HTTP |
| `KeyPrefix` | `string` | `"idp"` | Préfixe des clés Redis |
| `CompletedTtl` | `TimeSpan` | `24h` | Durée de vie des entrées complétées |
| `InProgressTtl` | `TimeSpan` | `30s` | Durée du lock InProgress |
| `ExecutionTimeout` | `TimeSpan` | `25s` | Timeout du handler métier (doit être < `InProgressTtl`) |
| `MaxBodySizeBytes` | `int` | `1 048 576` | Limite de lecture du body pour le hachage |
| `ShouldCacheStatusCode` | `Func<int, bool>` | 2xx + {400,404,409,410,422} | Filtre les codes mis en cache |

## Architecture

```text
src/
  Granit.Idempotency/
  ├── Abstractions/
  │   ├── IIdempotencyMetadata.cs     (contrat metadata endpoint)
  │   └── IIdempotencyStore.cs        (TryAcquire / Get / SetCompleted / Delete)
  ├── Attributes/
  │   └── IdempotentAttribute.cs      ([Idempotent], IIdempotencyMetadata)
  ├── Extensions/
  │   ├── IdempotencyApplicationBuilderExtensions.cs  (UseGranitIdempotency)
  │   └── IdempotencyServiceCollectionExtensions.cs   (AddGranitIdempotency)
  ├── Internal/
  │   ├── IdempotencyJsonContext.cs   (source-generated JSON)
  │   ├── IdempotencyMiddleware.cs    (machine à états, SHA-256, capture réponse)
  │   └── IdempotencyOptionsValidator.cs
  ├── Models/
  │   ├── IdempotencyEntry.cs         (entrée Redis sérialisée)
  │   ├── IdempotencyOptions.cs
  │   └── IdempotencyState.cs         (InProgress | Completed)
  ├── Redis/
  │   └── RedisIdempotencyStore.cs    (SET NX PX / SET XX PX + chiffrement AES)
  └── GranitIdempotencyModule.cs

tests/
  Granit.Idempotency.Tests/
  └── IdempotencyMiddlewareTests.cs   (4 scénarios critiques)
```

## Services enregistrés

### `GranitIdempotencyModule`

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IIdempotencyStore` | `RedisIdempotencyStore` | Scoped |
| `IValidateOptions<IdempotencyOptions>` | `IdempotencyOptionsValidator` | Singleton |
| `RecyclableMemoryStreamManager` | (natif) | Singleton |
| `IdempotencyMiddleware` | — | Transient (IMiddleware) |

`IIdempotencyStore` est `Scoped` pour aligner son cycle de vie avec les services
`ICurrentUserService` et `ICurrentTenant` injectés dans le middleware.

`RecyclableMemoryStreamManager` est `Singleton` : il gère un pool de buffers
partagé entre tous les threads, ce qui est son mode d'utilisation normal.

## Sécurité

- La **clé Redis** inclut `tenantId + userId` : un tenant ne peut pas rejouer la
  réponse d'un autre tenant même avec la même `Idempotency-Key`.
- La valeur du header `Idempotency-Key` est **hachée** (SHA-256) avant d'être
  incluse dans la clé Redis, évitant toute injection de caractères spéciaux.
- Les entrées Redis sont **chiffrées AES-256-CBC** via `ICacheValueEncryptor`
  (fourni par `Granit.Caching`) — obligatoire pour la
  conformité HDS sur les corps de réponse contenant des données de santé.
- Le middleware n'est **jamais déclenché** sur les endpoints sans `[Idempotent]`
  (vérification via `IIdempotencyMetadata` dans les endpoint metadata).
