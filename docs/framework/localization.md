<<<<<<< HEAD
# Localisation

Foundation n'introduit aucune abstraction de localisation. La localisation repose
entièrement sur `Microsoft.Extensions.Localization.IStringLocalizer<T>`, le système
standard d'ASP.NET Core. Contrairement à ABP qui propose un système de ressources
virtuelles, Foundation délègue la gestion des traductions à l'application hôte.

> **Référence Microsoft** :
> [Localisation dans ASP.NET Core](https://learn.microsoft.com/fr-fr/aspnet/core/fundamentals/localization)

## Contexte Guava Health

Guava Health est une application de santé numérique opérant en Belgique, avec trois
cultures cibles :

| Culture | Usage |
| --- | --- |
| `fr-BE` (défaut) | Interface principale, professionnels de santé francophones |
| `nl-BE` | Professionnels de santé néerlandophones |
| `en` | Fallback API, intégrations FHIR, logs techniques |

> Les logs applicatifs et les messages d'erreur internes (non affichés à l'utilisateur)
> restent **en anglais** pour la cohérence avec les outils de monitoring (Grafana, Loki).
=======
# Localization

`DigitalDynamics.Foundation.Localization` fournit un système de localisation JSON modulaire. Il s'intègre avec `IStringLocalizer<T>` de
`Microsoft.Extensions.Localization` et ajoute : ressources embarquées par assembly,
héritage inter-modules, culture fallback natif via `CultureInfo.Parent`, et cache
thread-safe.

## Pourquoi une localisation modulaire ?

Dans une architecture multi-packages (monorepo fondation + applications), chaque package
doit pouvoir embarquer ses propres traductions sans couplage. `Microsoft.Extensions.Localization`
standard repose sur des fichiers `.resx` ou des conventions de répertoires qui ne conviennent
pas aux packages NuGet. Ce module apporte :

- **Ressources embarquées** : les fichiers JSON sont compilés dans l'assembly du package
  (`EmbeddedResource`), sans dépendance à un chemin de fichier externe
- **Héritage** : un module applicatif peut hériter des traductions d'un module fondation
  et les surcharger ou les étendre
- **Culture fallback** : résolution automatique `fr-BE` → `fr` → culture par défaut,
  via `CultureInfo.Parent` (standard .NET, pas de logique manuelle)
- **Compatible `IStringLocalizer<T>`** : aucun changement dans le code applicatif qui
  utilise déjà `IStringLocalizer`
>>>>>>> feature/settings-module

## Installation

```bash
<<<<<<< HEAD
dotnet add package Microsoft.Extensions.Localization
```

ASP.NET Core inclut la localisation via le framework — aucun package NuGet supplémentaire
n'est nécessaire pour les projets `web` ou `webapi`.

## Configuration dans Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Enregistrement des services de localisation
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

// 2. Optionnel : localisation des DataAnnotations (validation)
builder.Services.AddMvc()
    .AddDataAnnotationsLocalization();

await builder.AddFoundationAsync<GuavaHostModule>();

var app = builder.Build();

// 3. Middleware de localisation (avant UseAuthentication)
var supportedCultures = new[] { "fr-BE", "nl-BE", "en" };
app.UseRequestLocalization(options =>
{
    options
        .SetDefaultCulture("fr-BE")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

app.UseAuthentication();
app.UseMultiTenancy();
app.UseAuthorization();

await app.UseFoundationAsync();
await app.RunAsync();
```

## Ressources de localisation

### Structure des fichiers

Les fichiers de ressources sont des fichiers `.resx` (XML) placés dans le dossier
`Resources/` de l'application, avec un fichier par culture :

```text
src/Guava.Host/
├── Resources/
│   ├── Modules.Auth.Messages.fr-BE.resx    (français Belgique — défaut)
│   ├── Modules.Auth.Messages.nl-BE.resx    (néerlandais Belgique)
│   └── Modules.Auth.Messages.en.resx       (anglais — fallback)
└── ...
```

Le nom du fichier correspond au **chemin du type** avec les `.` remplacés par des `/` :
`Modules.Auth.Messages` → classe `Modules.Auth.Messages`.

### Fichier .resx (exemple `fr-BE`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="PatientCreated" xml:space="preserve">
    <value>Dossier patient créé avec succès.</value>
  </data>
  <data name="ConsentRequired" xml:space="preserve">
    <value>Le consentement du patient est requis pour accéder à ce dossier.</value>
  </data>
  <data name="InvalidFhirResource" xml:space="preserve">
    <value>La ressource FHIR fournie est invalide : {0}.</value>
  </data>
</root>
```

### Fichier .resx (exemple `nl-BE`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="PatientCreated" xml:space="preserve">
    <value>Patiëntendossier succesvol aangemaakt.</value>
  </data>
  <data name="ConsentRequired" xml:space="preserve">
    <value>De toestemming van de patiënt is vereist om toegang te krijgen tot dit dossier.</value>
  </data>
</root>
```

## Utilisation

### Déclarer la classe de ressource marqueur

Créer une classe vide qui sert d'ancre pour `IStringLocalizer<T>` :

```csharp
namespace Guava.Modules.Auth;

// Classe marqueur — lie IStringLocalizer<AuthMessages> aux fichiers .resx
public sealed class AuthMessages { }
```

### Dans un service ou handler

```csharp
using Microsoft.Extensions.Localization;

public class ConsentService
{
    private readonly IStringLocalizer<AuthMessages> _localizer;
    private readonly ILogger<ConsentService> _logger;

    public ConsentService(
        IStringLocalizer<AuthMessages> localizer,
        ILogger<ConsentService> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<Result> RecordConsentAsync(Guid patientId, ConsentType type)
    {
        if (!await HasValidConsentAsync(patientId))
        {
            // Message localisé retourné à l'utilisateur
            return Result.Failure(_localizer["ConsentRequired"]);
        }

        _logger.LogInformation("Consent recorded for patient {PatientId}", patientId);

        return Result.Success(_localizer["PatientCreated"]);
    }
}
```

### Dans un handler Wolverine (method injection)

```csharp
public static async Task<IResult> Handle(
    GetPatientQuery query,
    AppDbContext db,
    IStringLocalizer<AuthMessages> localizer,
    ICurrentUserService currentUser,
    CancellationToken cancellationToken)
{
    if (!currentUser.IsInRole("practitioner"))
    {
        return Results.Forbid();
    }

    var patient = await db.Patients.FindAsync(query.PatientId, cancellationToken);
    if (patient is null)
    {
        return Results.NotFound(new { Message = localizer["PatientNotFound"].Value });
    }

    return Results.Ok(patient.ToDto());
}
```

### Dans un endpoint minimal

```csharp
app.MapPost("/api/consent", async (
    RecordConsentRequest request,
    ConsentService service,
    IStringLocalizer<AuthMessages> localizer) =>
{
    var result = await service.RecordConsentAsync(request.PatientId, request.Type);

    return result.IsSuccess
        ? Results.Ok(new { Message = localizer["ConsentRecorded"].Value })
        : Results.BadRequest(new { Message = result.Error });
});
```

## Détection de la culture

ASP.NET Core détecte la culture de la requête depuis plusieurs sources dans cet ordre :

| Source | Exemple | Usage |
| --- | --- | --- |
| Query string | `?culture=nl-BE` | Tests, liens directs |
| Cookie | `c=fr-BE` | Préférence utilisateur persistée |
| Header `Accept-Language` | `Accept-Language: nl-BE,nl;q=0.9` | Navigateurs |
| Défaut | `fr-BE` | Fallback si rien n'est détecté |

Pour une API REST sans UI, l'en-tête `Accept-Language` est la méthode standard.

## Culture dans les modules Foundation

La culture courante est accessible via `CultureInfo.CurrentUICulture` partout dans
le code, sans injection de dépendances. `UseRequestLocalization` l'initialise
automatiquement à chaque requête :

```csharp
// Culture courante (initialisée par le middleware)
var culture = CultureInfo.CurrentUICulture.Name;  // "fr-BE", "nl-BE", "en"
```

## Fallback de culture

Quand une clé n'existe pas dans la culture demandée, ASP.NET Core cherche dans cet ordre :

```text
nl-BE  →  nl  →  (culture neutre)  →  [ResourceNotFound]
```

Pour garantir un fallback vers l'anglais quand une traduction `nl-BE` est absente :
ne pas créer de fichier `nl.resx` — la clé sera retournée telle quelle (son nom),
ce qui est acceptable pour une API REST.

## Localisation des réponses d'erreur

Pour les API REST, les messages d'erreur localisés peuvent être inclus dans les
réponses `ProblemDetails` :

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var localizer = context.HttpContext.RequestServices
            .GetRequiredService<IStringLocalizer<AuthMessages>>();

        if (context.ProblemDetails.Status == StatusCodes.Status403Forbidden)
        {
            context.ProblemDetails.Detail = localizer["AccessDenied"];
        }
    };
});
```

## Tests

```csharp
// Option 1 : NSubstitute
var localizer = Substitute.For<IStringLocalizer<AuthMessages>>();
localizer["ConsentRequired"].Returns(new LocalizedString("ConsentRequired",
    "Consent required"));

var service = new ConsentService(localizer, NullLogger<ConsentService>.Instance);

// Option 2 : StringLocalizer réel avec ressources de test
// Utiliser Microsoft.Extensions.Localization dans un IServiceCollection de test
var services = new ServiceCollection();
services.AddLocalization(o => o.ResourcesPath = "Resources");
var provider = services.BuildServiceProvider();
var localizer = provider.GetRequiredService<IStringLocalizer<AuthMessages>>();
=======
dotnet add package DigitalDynamics.Foundation.Localization
```

## Configuration

### Avec le système de modules (recommandé)

Ajouter `[DependsOn(typeof(FoundationLocalizationModule))]` sur le module applicatif :

```csharp
[DependsOn(typeof(FoundationLocalizationModule))]
public sealed class MyAppModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FoundationLocalizationOptions>(options =>
        {
            options.Resources
                .Add<MyAppResource>(defaultCulture: "fr")
                .AddJson(
                    typeof(MyAppModule).Assembly,
                    "MyApp.Localization.MyApp")
                .AddBaseTypes(typeof(FoundationLocalizationResource));

            options.DefaultResourceType = typeof(MyAppResource);
            options.Languages.Add(new LanguageInfo("fr", "Français"));
            options.Languages.Add(new LanguageInfo("en", "English"));
        });
    }
}
```

### Enregistrement direct

Pour les projets qui n'utilisent pas le système de modules :

```csharp
builder.Services.AddFoundationLocalization(options =>
{
    options.Resources
        .Add<MyAppResource>(defaultCulture: "fr")
        .AddJson(typeof(Program).Assembly, "MyApp.Localization.MyApp")
        .AddBaseTypes(typeof(FoundationLocalizationResource));
});
```

## Fichiers JSON de traduction

### Format

Chaque fichier JSON contient les traductions pour **une seule culture**. Le nom du fichier
est conventionnellement `{culture}.json` (ex. `fr.json`, `en.json`, `fr-BE.json`).

```json
{
  "culture": "fr",
  "texts": {
    "MyKey": "Ma traduction",
    "Error:NotFound": "Ressource introuvable",
    "Validation": {
      "Required": "Ce champ est obligatoire.",
      "MaxLength": "Maximum {0} caractères."
    }
  }
}
```

La propriété `culture` doit correspondre à un identifiant de culture .NET valide
(ex. `"fr"`, `"fr-BE"`, `"en"`, `"en-US"`).

### Clés imbriquées

Les objets JSON imbriqués sont aplatis avec `.` comme séparateur. Les deux formes
sont équivalentes :

```json
// Forme plate
{ "texts": { "Validation.Required": "Ce champ est obligatoire." } }

// Forme imbriquée (aplatie en Validation.Required)
{ "texts": { "Validation": { "Required": "Ce champ est obligatoire." } } }
```

> **Attention** : mélanger les deux formes pour la même clé dans le même fichier
> provoque une `InvalidOperationException` (détection de collision).

### Structure recommandée

```text
src/MyApp/
└── Localization/
    └── MyApp/
        ├── fr.json
        ├── en.json
        └── fr-BE.json
```

Le fichier `.csproj` doit inclure les fichiers comme ressources embarquées :

```xml
<ItemGroup>
  <EmbeddedResource Include="Localization\**\*.json" />
</ItemGroup>
```

Le préfixe de ressource passé à `AddJson()` correspond au namespace .NET des fichiers
embarqués (les `/` deviennent des `.`) : `"MyApp.Localization.MyApp"`.

## Classes marker de ressource

Chaque ressource est représentée par une classe vide annotée avec des attributs :

```csharp
using DigitalDynamics.Foundation.Localization.Attributes;

[LocalizationResourceName("MyApp")]
public sealed class MyAppResource;
```

### Héritage par attribut

L'attribut `[InheritResource]` déclare un héritage statique (connu à la compilation) :

```csharp
[LocalizationResourceName("MyApp")]
[InheritResource(typeof(FoundationLocalizationResource))]
public sealed class MyAppResource;
```

L'héritage par attribut est automatiquement détecté par `JsonStringLocalizerFactory`
lors de la création du localizer.

### Héritage par API fluent

L'API fluent `AddBaseTypes()` permet un héritage dynamique (défini à l'enregistrement) :

```csharp
options.Resources
    .Add<MyAppResource>(defaultCulture: "fr")
    .AddJson(assembly, "MyApp.Localization.MyApp")
    .AddBaseTypes(typeof(FoundationLocalizationResource));
```

Les deux mécanismes peuvent être combinés. Les clés sont résolues dans cet ordre :
ressource courante → ressources parentes (dans l'ordre de déclaration).

## IStringLocalizer

### Injection par constructeur

```csharp
public sealed class PatientService
{
    private readonly IStringLocalizer<MyAppResource> _localizer;

    public PatientService(IStringLocalizer<MyAppResource> localizer)
    {
        _localizer = localizer;
    }

    public string GetNotFoundMessage(Guid id)
        => _localizer["Patient:NotFound", id];
}
```

### Traduction simple

```csharp
string message = localizer["MyKey"];
// → "Ma traduction" (si trouvé en culture courante)
// → "MyKey" (si non trouvé — la clé est retournée telle quelle)
```

### Traduction avec paramètres

Les paramètres `{0}`, `{1}`, etc. sont substitués via `string.Format` :

```csharp
string message = localizer["Validation.MaxLength", 100];
// → "Maximum 100 caractères."
```

> Quand aucun argument n'est passé, `string.Format` est bypassé pour optimiser les
> performances (pas d'allocation inutile).

### Vérifier si une traduction existe

```csharp
LocalizedString result = localizer["MyKey"];
if (!result.ResourceNotFound)
{
    // Traduction trouvée
    string value = result.Value;
}
```

### Lister toutes les traductions

```csharp
IEnumerable<LocalizedString> all = localizer.GetAllStrings(includeParentCultures: true);
```

## Culture fallback

La résolution d'une clé suit cet ordre :

1. **Culture courante** (`CultureInfo.CurrentUICulture`) — ex. `fr-BE`
2. **Culture parente** (`CultureInfo.Parent`) — ex. `fr`
3. **Ancêtres** jusqu'à `CultureInfo.InvariantCulture`
4. **Culture par défaut de la ressource** (`defaultCulture` dans `Add<T>()`) — ex. `fr`
5. **Ressources parentes** (héritage) — dans l'ordre de déclaration

Ce comportement repose sur `CultureInfo.Parent` standard .NET : aucune logique manuelle
de parsing de culture name.

### Définir la culture par requête

Configurer la culture via `RequestLocalizationMiddleware` :

```csharp
app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("fr");
    options.AddSupportedCultures("fr", "en", "fr-BE");
    options.AddSupportedUICultures("fr", "en", "fr-BE");
});
```

La culture peut aussi être définie manuellement dans un middleware :

```csharp
app.Use(async (context, next) =>
{
    string? lang = context.Request.Headers["Accept-Language"].FirstOrDefault();
    if (!string.IsNullOrEmpty(lang))
    {
        CultureInfo.CurrentUICulture = new CultureInfo(lang);
    }
    await next();
});
```

## Messages LoggerMessage

Les templates `[LoggerMessage]` sont des **identifiants stables** utilisés comme clés
de corrélation dans Loki/Grafana. Ils ne doivent **pas** être traduits.

La localisation s'applique uniquement aux messages **orientés utilisateur** (UI, API
responses, erreurs métier) :

```csharp
// Template LoggerMessage stable — NE PAS traduire
[LoggerMessage(Level = LogLevel.Debug, Message = "Lease renewal in {Delay}")]
private static partial void LogNextRenewal(ILogger logger, TimeSpan delay);

// Message utilisateur — utiliser IStringLocalizer
public string GetExpiredMessage()
    => _localizer["Lease:Expired"];
```

## FoundationLocalizationOptions

```csharp
public sealed class FoundationLocalizationOptions
{
    public LocalizationResourceStore Resources { get; }
    public Type? DefaultResourceType { get; set; }
    public List<LanguageInfo> Languages { get; }
}
```

| Propriété | Type | Description |
| --- | --- | --- |
| `Resources` | `LocalizationResourceStore` | Registre des ressources déclarées |
| `DefaultResourceType` | `Type?` | Ressource fallback quand le type exact n'est pas enregistré |
| `Languages` | `List<LanguageInfo>` | Langues disponibles (pour un sélecteur de langue en UI) |

## LocalizationResourceStore

Registre thread-safe des ressources déclarées. Méthodes principales :

```csharp
// Enregistrer une ressource
LocalizationResourceInfo info = options.Resources.Add<MyAppResource>(defaultCulture: "fr");

// Récupérer une ressource (lève InvalidOperationException si absente)
LocalizationResourceInfo info = options.Resources.Get<MyAppResource>();

// Récupérer sans exception
bool found = options.Resources.TryGetValue(typeof(MyAppResource), out LocalizationResourceInfo? info);

// Lister toutes les ressources enregistrées
IReadOnlyCollection<LocalizationResourceInfo> all = options.Resources.GetAll();
```

## LanguageInfo

Représente une langue disponible dans l'application :

```csharp
public sealed class LanguageInfo
{
    public string CultureName { get; }    // ex: "fr-BE"
    public string DisplayName { get; }    // ex: "Français (Belgique)"
    public string? FlagIcon { get; }      // ex: "🇧🇪" (optionnel)
}
```

Utilisé pour peupler un sélecteur de langue en interface utilisateur.

## Ressource Foundation intégrée

`FoundationLocalizationResource` est la ressource de base fournie par ce package.
Elle contient les messages d'erreur communs à tous les modules :

| Clé | FR | EN |
| --- | --- | --- |
| `Foundation:EntityNotFound` | L'entité de type {0} avec l'identifiant {1} n'a pas été trouvée. | Entity of type {0} with identifier {1} was not found. |
| `Foundation:ValidationError` | Erreur de validation. | Validation error. |
| `Foundation:Unauthorized` | Accès non autorisé. | Unauthorized access. |
| `Foundation:Forbidden` | Accès interdit. | Forbidden. |
| `Foundation:InternalError` | Une erreur interne est survenue. | An internal error occurred. |

Les modules applicatifs peuvent hériter de `FoundationLocalizationResource` pour
réutiliser ces messages sans les redéfinir.

## Architecture

```text
DigitalDynamics.Foundation.Localization
├── Attributes/
│   ├── LocalizationResourceNameAttribute.cs   (nom court de la ressource)
│   └── InheritResourceAttribute.cs            (héritage statique par attribut)
├── Json/
│   ├── JsonLocalizationDictionaryBuilder.cs   (parseur JSON → dictionnaire culture)
│   ├── JsonStringLocalizer.cs                 (IStringLocalizer avec cache Lazy<T>)
│   └── JsonStringLocalizerFactory.cs          (IStringLocalizerFactory, cache par type)
├── Extensions/
│   └── LocalizationServiceCollectionExtensions.cs  (AddFoundationLocalization)
├── EmbeddedJsonSource.cs                      (Assembly + préfixe de ressource)
├── FoundationLocalizationModule.cs            (module Foundation)
├── FoundationLocalizationOptions.cs           (options de configuration)
├── FoundationLocalizationResource.cs          (marker — ressource de base)
├── LanguageInfo.cs                            (info langue pour UI)
├── LocalizationResourceInfo.cs               (info ressource + API fluent)
├── LocalizationResourceStore.cs              (registre des ressources)
└── Localization/
    └── Foundation/
        ├── fr.json
        └── en.json
```

## Services enregistrés

| Service | Implémentation | Lifetime | Notes |
| --- | --- | --- | --- |
| `IStringLocalizerFactory` | `JsonStringLocalizerFactory` | Singleton | Cache par type via `ConcurrentDictionary<Type, Lazy<IStringLocalizer>>` |
| `IStringLocalizer<T>` | `StringLocalizer<T>` | Transient | Délègue à `IStringLocalizerFactory` |

Tous les enregistrements utilisent `TryAdd*` pour permettre le remplacement dans les tests.

## Tests

### Test d'une traduction

```csharp
[Fact]
public void GivenFrenchCulture_WhenLocalizing_ThenReturnsFrenchTranslation()
{
    ServiceCollection services = new();
    services.AddFoundationLocalization(options =>
    {
        options.Resources
            .Add<MyAppResource>(defaultCulture: "fr")
            .AddJson(typeof(MyAppResource).Assembly, "MyApp.Localization.MyApp");
    });

    ServiceProvider provider = services.BuildServiceProvider();
    IStringLocalizer<MyAppResource> localizer =
        provider.GetRequiredService<IStringLocalizer<MyAppResource>>();

    using IDisposable _ = new CultureScope("fr");

    localizer["MyKey"].Value.Should().Be("Ma traduction");
    localizer["MyKey"].ResourceNotFound.Should().BeFalse();
}
```

### Test d'une clé absente

```csharp
[Fact]
public void GivenMissingKey_WhenLocalizing_ThenReturnsKeyAsValue()
{
    // ...
    LocalizedString result = localizer["NonExistentKey"];
    result.Value.Should().Be("NonExistentKey");
    result.ResourceNotFound.Should().BeTrue();
}
```

### Changer la culture dans les tests

Utiliser `CultureInfo.CurrentUICulture` dans un scope :

```csharp
CultureInfo previous = CultureInfo.CurrentUICulture;
try
{
    CultureInfo.CurrentUICulture = new CultureInfo("en");
    localizer["MyKey"].Value.Should().Be("My translation");
}
finally
{
    CultureInfo.CurrentUICulture = previous;
}
```

### Mock IStringLocalizer avec NSubstitute

Pour les tests qui ne ciblent pas la localisation elle-même :

```csharp
IStringLocalizer<MyAppResource> localizer = Substitute.For<IStringLocalizer<MyAppResource>>();
localizer["PatientNotFound", Arg.Any<object[]>()]
    .Returns(new LocalizedString("PatientNotFound", "Patient introuvable."));
>>>>>>> feature/settings-module
```

## Bonnes pratiques

<<<<<<< HEAD
1. **Clés descriptives** — utiliser des clés en PascalCase décrivant le concept métier
   (`ConsentRequired`, `PatientNotFound`), pas des phrases entières
2. **Anglais par défaut dans les logs** — les messages de log restent en anglais
   (voir [logging.md](logging.md)) ; seuls les messages utilisateur sont localisés
3. **Pas de PII dans les ressources** — les fichiers `.resx` ne contiennent que des
   templates (pas de données patient)
4. **Un namespace de ressource par module** — `Guava.Modules.Auth.AuthMessages`,
   `Guava.Modules.Fhir.FhirMessages`, etc.
5. **Fallback gracieux** — si `IStringLocalizer` ne trouve pas une clé, il retourne
   la clé elle-même ; les clés doivent donc être lisibles comme texte de fallback
6. **`fr-BE` comme culture par défaut** — obligation légale HDS EXI-07/08 pour les
   interfaces utilisateur destinées aux professionnels de santé en Belgique
=======
1. **Un marker par module** — créer une classe marker par package/module pour isoler
   les espaces de noms des clés
2. **Préfixer les clés** — utiliser un préfixe cohérent pour éviter les collisions
   (ex. `"Foundation:EntityNotFound"`, `"Patient:NotFound"`)
3. **Hériter `FoundationLocalizationResource`** — pour bénéficier des messages d'erreur
   de base sans les redupliquer
4. **Ne pas traduire les templates LoggerMessage** — garder les templates stables pour
   la corrélation dans Loki ; traduire uniquement les messages utilisateur
5. **Toujours fournir `fr.json` et `en.json`** — au minimum ces deux cultures pour la
   compatibilité applicative
6. **Clés en anglais** — nommer les clés en anglais pour la lisibilité du code, même
   si la culture par défaut est le français
7. **Culture fallback via middleware** — configurer `RequestLocalizationMiddleware`
   plutôt que de manipuler `CultureInfo.CurrentUICulture` manuellement

## Dépendances

| Package | Rôle |
| --- | --- |
| `Microsoft.Extensions.Localization` | `IStringLocalizer`, `IStringLocalizerFactory`, `StringLocalizer<>` |
| `Microsoft.Extensions.Options` | `IOptions<FoundationLocalizationOptions>` |
| `DigitalDynamics.Foundation.Core` | Système de modules (`FoundationModule`, `[DependsOn]`) |
>>>>>>> feature/settings-module
