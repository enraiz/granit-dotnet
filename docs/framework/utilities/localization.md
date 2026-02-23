# Localization

`Granit.Localization` fournit un système de localisation JSON modulaire. Il s'intègre avec `IStringLocalizer<T>` de
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

## Installation

```bash
dotnet add package Granit.Localization
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
using Granit.Localization.Attributes;

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
Granit.Localization
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
```

## Bonnes pratiques

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
| `Granit.Core` | Système de modules (`FoundationModule`, `[DependsOn]`) |
