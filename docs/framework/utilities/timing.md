# Timing

`Granit.Timing` fournit une abstraction pour l'accès au temps système
et les conversions de fuseau horaire. Il remplace tous les appels directs à
`DateTimeOffset.UtcNow` dans le code applicatif.

Inspiré du module [`Volo.Abp.Timing`](https://abp.io/docs/latest/framework/infrastructure/timing)
d'ABP Framework.

## Pourquoi une abstraction pour le temps ?

Gérer le temps correctement dans une application distribuée est plus complexe qu'il n'y
paraît. Utiliser directement `DateTime.Now` ou `DateTimeOffset.UtcNow` pose plusieurs
problèmes :

- **Heure locale du serveur** : `DateTime.Now` retourne l'heure locale de la machine,
  qui peut différer entre serveurs d'un même cluster ou entre environnements
  (développement, staging, production)
- **Absence d'information de fuseau** : `DateTime.Now` ne contient pas d'information
  de fuseau horaire exploitable, ce qui rend les conversions fragiles
- **Testabilité** : les appels statiques à `DateTime.Now` ou `DateTimeOffset.UtcNow` ne
  sont pas mockables, ce qui oblige les tests à utiliser des assertions approximatives
  (`BeCloseTo`) au lieu d'assertions exactes
- **Incohérence** : sans point centralisé, chaque développeur peut utiliser `DateTime.Now`,
  `DateTime.UtcNow`, `DateTimeOffset.Now` ou `DateTimeOffset.UtcNow` de manière
  inconsistante dans la codebase

> **Ne jamais utiliser `DateTime.Now`, `DateTime.UtcNow` ou `DateTimeOffset.UtcNow`
> directement dans le code applicatif !**
>
> Utiliser `IClock.Now` à la place pour garantir la cohérence, la testabilité et le
> support des fuseaux horaires.

### DateTimeOffset plutôt que DateTime

Digital Dynamics utilise `DateTimeOffset` (et non `DateTime`) comme type de référence
pour les dates et heures. `DateTimeOffset` embarque toujours l'offset UTC, éliminant
toute ambiguïté sur le fuseau horaire représenté. C'est aussi le type recommandé par
PostgreSQL (`timestamptz`).

## Installation

```bash
dotnet add package Granit.Timing
```

## Configuration

### Avec le système de modules (recommandé)

Le module `GranitTimingModule` est automatiquement chargé via `[DependsOn]` quand
un module dépendant (ex : Persistence) en a besoin. Il suffit d'utiliser
`AddGranit<T>()` dans `Program.cs` (voir [modularity.md](../core/modularity.md)).

### Enregistrement direct

Pour les projets qui n'utilisent pas le système de modules :

```csharp
builder.Services.AddGranitTiming();
```

Avec options :

```csharp
builder.Services.AddGranitTiming(options =>
{
    options.DefaultTimezone = "Europe/Brussels";
});
```

## IClock

`IClock` est l'interface principale du module, définie dans le package
`Granit.Timing`. Elle doit être injectée partout où le code a besoin
de l'heure courante ou de conversions de fuseau horaire.

```csharp
namespace Granit.Timing;

public interface IClock
{
    DateTimeOffset Now { get; }
    bool SupportsMultipleTimezone { get; }
    DateTimeOffset Normalize(DateTimeOffset dateTime);
    DateTimeOffset ConvertToUserTime(DateTimeOffset utcDateTime);
    DateTimeOffset ConvertToUtc(DateTimeOffset dateTime);
}
```

### Membres

| Membre | Description |
| --- | --- |
| `Now` | Retourne l'instant présent en UTC (`Offset = TimeSpan.Zero`) |
| `SupportsMultipleTimezone` | `true` — indique que le clock supporte les conversions multi-fuseaux |
| `Normalize` | Convertit un `DateTimeOffset` en UTC |
| `ConvertToUserTime` | Convertit un `DateTimeOffset` UTC vers le fuseau de l'utilisateur courant |
| `ConvertToUtc` | Convertit un `DateTimeOffset` (potentiellement avec offset local) vers UTC |

### IClock vs TimeProvider

.NET 8 a introduit `System.TimeProvider` qui fournit une abstraction sur
`DateTimeOffset.UtcNow`. Notre `IClock` utilise `TimeProvider` en interne pour `Now`
et ajoute les opérations timezone que `TimeProvider` ne couvre pas :

| Fonctionnalité | TimeProvider | IClock |
| --- | --- | --- |
| Obtenir l'heure UTC | Oui | Oui (délègue à TimeProvider) |
| Testabilité (FakeTimeProvider) | Oui | Oui |
| Conversion timezone utilisateur | Non | Oui |
| Normalisation UTC | Non | Oui |
| Contexte timezone per-request | Non | Oui |

`IClock` est un surensemble de `TimeProvider`. Pour le code qui n'a besoin que de
l'heure courante sans conversion de fuseau, `TimeProvider` suffit. Pour le code
applicatif Digital Dynamics, préférer `IClock` pour sa cohérence et ses capacités
de conversion.

## Now

`IClock.Now` retourne l'instant présent en UTC (`DateTimeOffset` avec `Offset = TimeSpan.Zero`).

```csharp
var now = clock.Now;
// now.Offset == TimeSpan.Zero (toujours)
```

En interne, `Clock.Now` délègue à `TimeProvider.GetUtcNow()`. Cela garantit que
`FakeTimeProvider` (du package `Microsoft.Extensions.TimeProvider.Testing`) fonctionne
dans les tests.

## Normalize

`Normalize` convertit un `DateTimeOffset` en UTC. Si la valeur est déjà en UTC,
elle est retournée inchangée.

```csharp
// DateTimeOffset avec offset local (+02:00, Europe/Brussels en été)
var local = new DateTimeOffset(2026, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
var utc = clock.Normalize(local);
// utc == 2026-06-15T12:30:00+00:00
```

Cas d'usage : garantir que toute date persistée est en UTC, même si un développeur
passe par erreur un `DateTimeOffset` avec un offset local.

> La normalisation peut être appliquée automatiquement dans les pipelines de
> sérialisation (model binding ASP.NET Core, intercepteurs EF Core) pour convertir
> les dates entrantes en UTC avant persistance. Voir la section
> [DisableDateTimeNormalization](#disabledatetimenormalization) pour les exceptions.

## ConvertToUserTime

`ConvertToUserTime` convertit un `DateTimeOffset` UTC vers le fuseau horaire de
l'utilisateur courant (défini par `ICurrentTimezoneProvider`).

```csharp
// UTC
var utcTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

// Conversion vers le fuseau de l'utilisateur (Europe/Brussels = UTC+2 en été)
var userTime = clock.ConvertToUserTime(utcTime);
// userTime == 2026-06-15T14:00:00+02:00
```

Si aucun fuseau n'est configuré (`ICurrentTimezoneProvider.Timezone` est `null`),
la valeur est retournée inchangée.

## ConvertToUtc

`ConvertToUtc` convertit un `DateTimeOffset` vers UTC.

```csharp
var localTime = new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.FromHours(2));
var utcTime = clock.ConvertToUtc(localTime);
// utcTime == 2026-06-15T12:00:00+00:00
```

## ICurrentTimezoneProvider

`ICurrentTimezoneProvider` porte le fuseau horaire de l'utilisateur courant.
L'implémentation par défaut utilise `AsyncLocal<string?>` pour isoler la valeur
par contexte async (per-request).

```csharp
public interface ICurrentTimezoneProvider
{
    string? Timezone { get; set; }
}
```

### Per-request timezone

Le fuseau horaire est typiquement défini au début de chaque requête HTTP, par exemple
dans un middleware :

```csharp
app.Use(async (context, next) =>
{
    var timezoneProvider = context.RequestServices
        .GetRequiredService<ICurrentTimezoneProvider>();

    // Lire le fuseau depuis un header, un claim, ou le profil utilisateur
    var timezone = context.Request.Headers["X-Timezone"].FirstOrDefault();
    if (!string.IsNullOrEmpty(timezone))
    {
        timezoneProvider.Timezone = timezone;
    }

    await next();
});
```

L'ordre de résolution recommandé pour le fuseau horaire est :

1. **Header HTTP** `X-Timezone` (pour les SPA et clients API)
2. **Claim utilisateur** dans le token JWT (pour les utilisateurs authentifiés)
3. **Profil utilisateur** en base de données (configuration persistante)
4. **`DefaultTimezone`** des `ClockOptions` (fallback global)

### Registration DI

`CurrentTimezoneProvider` est enregistré en **Singleton**. Le stockage est géré par
le runtime via `AsyncLocal<T>`, qui isole automatiquement les valeurs par contexte
d'exécution async. Chaque requête voit sa propre valeur sans interférence.

## ClockOptions

```csharp
public sealed class ClockOptions
{
    /// <summary>
    /// Fuseau horaire par défaut quand aucun n'est spécifié par l'utilisateur.
    /// Null = pas de conversion (les dates restent en UTC).
    /// Exemple : "Europe/Brussels"
    /// </summary>
    public string? DefaultTimezone { get; set; }
}
```

| Propriété | Défaut | Description |
| --- | --- | --- |
| `DefaultTimezone` | `null` | Fuseau horaire IANA par défaut. Si `null`, aucune conversion n'est appliquée et les dates restent en UTC |

> Utiliser les identifiants de fuseau horaire IANA (ex. `"Europe/Brussels"`,
> `"Europe/Paris"`) et non les identifiants Windows (ex. `"Romance Standard Time"`).
> Les identifiants IANA sont le standard sur Linux et sont supportés nativement par
> .NET sur toutes les plateformes.

## DisableDateTimeNormalization

L'attribut `[DisableDateTimeNormalization]` peut être placé sur une classe, propriété
ou paramètre pour désactiver la normalisation automatique des dates dans les pipelines
de normalisation.

```csharp
[DisableDateTimeNormalization]
public DateTimeOffset OriginalTimestamp { get; set; }
```

Cas d'usage typiques :

- **Horodatages externes** : dates provenant de systèmes tiers dont l'offset original
  doit être préservé
- **Données d'audit** : dates qui doivent refléter exactement le moment tel que
  capturé, sans reconversion

## Usage

### Dans un handler Wolverine (method injection)

Wolverine injecte `IClock` comme paramètre de méthode (method injection) :

```csharp
public static class SyncUserProfileHandler
{
    public static async Task<UserProfileResponse> Handle(
        SyncUserProfile command,
        AuthDbContext db,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var profile = new UserProfile
        {
            LastLoginAt = clock.Now // UTC garanti
        };
        // ...
    }
}
```

### Dans un service avec injection par constructeur

```csharp
public class AppointmentService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public AppointmentService(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Guid> ScheduleAsync(
        Guid patientId,
        DateTimeOffset requestedTime,
        CancellationToken cancellationToken)
    {
        var appointment = new Appointment
        {
            PatientId = patientId,
            ScheduledAt = _clock.Normalize(requestedTime), // UTC garanti
            CreatedAt = _clock.Now
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }
}
```

### Dans un intercepteur EF Core

Les intercepteurs reçoivent `IClock` par injection constructeur :

```csharp
public sealed class AuditedEntityInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;

    public AuditedEntityInterceptor(
        ICurrentUserService currentUserService,
        IClock clock)
    {
        _clock = clock;
    }

    private void ApplyAuditFields(DbContext? context)
    {
        var now = _clock.Now; // UTC garanti
        // ...
    }
}
```

### Affichage en heure locale utilisateur

Pour afficher une date en heure locale dans une réponse API :

```csharp
public static AppointmentResponse ToResponse(
    Appointment appointment,
    IClock clock)
{
    return new AppointmentResponse
    {
        ScheduledAtUtc = appointment.ScheduledAt,
        ScheduledAtLocal = clock.ConvertToUserTime(appointment.ScheduledAt)
    };
}
```

## Bonnes pratiques

1. **Toujours injecter `IClock`** — ne jamais appeler `DateTime.Now`,
   `DateTime.UtcNow` ou `DateTimeOffset.UtcNow` directement dans le code applicatif
2. **Stocker en UTC** — persister toutes les dates en UTC (`DateTimeOffset` avec
   offset zéro) ; convertir vers le fuseau utilisateur uniquement à l'affichage
3. **Utiliser `DateTimeOffset`** — préférer `DateTimeOffset` à `DateTime` pour éviter
   toute ambiguïté sur le fuseau horaire représenté
4. **Utiliser les fuseaux IANA** — utiliser les identifiants IANA (`Europe/Brussels`)
   et non les identifiants Windows (`Romance Standard Time`)
5. **Normaliser les entrées** — appeler `clock.Normalize()` sur les dates provenant
   de l'extérieur (API, formulaires) avant de les persister
6. **Tester avec `FakeTimeProvider`** — figer le temps dans les tests pour des
   assertions déterministes et exactes, pas de `BeCloseTo`
7. **Définir le fuseau le plus tôt possible** — configurer `ICurrentTimezoneProvider`
   dans un middleware au début du pipeline HTTP

## Architecture

```text
Granit.Timing
├── IClock.cs                             (interface, contrat public)
├── ICurrentTimezoneProvider.cs           (interface, contrat public)
├── Clock.cs                              (implémentation, délègue à TimeProvider)
├── ClockOptions.cs                       (options configurables)
├── CurrentTimezoneProvider.cs            (AsyncLocal, Singleton)
├── DisableDateTimeNormalizationAttribute.cs
├── GranitTimingModule.cs             (module Granit)
└── Extensions/
    └── TimingServiceCollectionExtensions.cs  (AddGranitTiming)
```

## Services enregistrés

| Service | Implémentation | Lifetime | Notes |
| --- | --- | --- | --- |
| `TimeProvider` | `TimeProvider.System` | Singleton | Provider standard .NET |
| `ICurrentTimezoneProvider` | `CurrentTimezoneProvider` | Singleton | AsyncLocal isolé par requête |
| `IClock` | `Clock` | Singleton | Stateless, thread-safe |

Tous les enregistrements utilisent `TryAddSingleton` pour permettre le remplacement
dans les tests.

## Tests

### FakeTimeProvider

Pour les tests, utiliser `FakeTimeProvider` du package
`Microsoft.Extensions.TimeProvider.Testing` :

```csharp
var fakeTimeProvider = new FakeTimeProvider();
var timezoneProvider = Substitute.For<ICurrentTimezoneProvider>();
var clock = new Clock(fakeTimeProvider, timezoneProvider);

// Figer le temps
var fixedTime = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
fakeTimeProvider.SetUtcNow(fixedTime);

clock.Now.Should().Be(fixedTime);
```

### Mock IClock avec NSubstitute

Pour les tests de code applicatif, mocker directement `IClock` :

```csharp
var clock = Substitute.For<IClock>();
var fixedNow = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
clock.Now.Returns(fixedNow);

// Assertions exactes (pas de BeCloseTo)
entity.CreatedAt.Should().Be(fixedNow);
```

### Assertions exactes vs approximatives

Avant `IClock`, les tests utilisaient `BeCloseTo` avec une tolérance de 5 secondes :

```csharp
// Avant (fragile, tolérance 5s)
entity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

// Après (déterministe, assertion exacte)
entity.CreatedAt.Should().Be(fixedNow);
```

## Dépendances

| Package | Rôle |
| --- | --- |
| `Microsoft.Extensions.Options` | `IOptions<ClockOptions>` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | Registration DI |
