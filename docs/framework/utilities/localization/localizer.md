# IStringLocalizer — Granit.Localization

## Injection par constructeur

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

## Traduction simple

```csharp
string message = localizer["MyKey"];
// → "Ma traduction" (si trouvé en culture courante)
// → "MyKey" (si non trouvé — la clé est retournée telle quelle)
```

## Traduction avec paramètres

Les paramètres `{0}`, `{1}`, etc. sont substitués via
[SmartFormat.NET](https://github.com/axuno/SmartFormat) (drop-in compatible
`string.Format`) :

```csharp
string message = localizer["Validation.MaxLength", 100];
// → "Maximum 100 caractères."
```

> Quand aucun argument n'est passé, le formatage est bypassé pour optimiser les
> performances (pas d'allocation inutile).

## Pluralisation

SmartFormat.NET fournit la pluralisation automatique basée sur les règles CLDR. La
syntaxe utilise le pipe (`|`) pour séparer les formes : zéro, singulier, pluriel.

### Format JSON

```json
{
  "culture": "fr",
  "texts": {
    "Files:Count": "{0:Aucun fichier|Un fichier|{} fichiers}"
  }
}
```

```json
{
  "culture": "en",
  "texts": {
    "Files:Count": "{0:No file|One file|{} files}"
  }
}
```

Les trois formes séparées par `|` sont :

1. **Zéro** — quand l'argument vaut 0
2. **Singulier** — quand l'argument vaut 1
3. **Pluriel** — pour toutes les autres valeurs

`{}` (accolades vides) est remplacé par la valeur de l'argument courant.

### Utilisation en C\#

```csharp
string zero = localizer["Files:Count", 0];    // → "Aucun fichier"
string one = localizer["Files:Count", 1];      // → "Un fichier"
string many = localizer["Files:Count", 42];    // → "42 fichiers"
```

### Rétrocompatibilité

SmartFormat.NET est un surensemble de `string.Format`. Toutes les traductions
existantes utilisant `{0}`, `{1}`, etc. continuent de fonctionner sans modification.

## Vérifier si une traduction existe

```csharp
LocalizedString result = localizer["MyKey"];
if (!result.ResourceNotFound)
{
    // Traduction trouvée
    string value = result.Value;
}
```

## Lister toutes les traductions

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

## Tests

### Test d'une traduction

```csharp
[Fact]
public void GivenFrenchCulture_WhenLocalizing_ThenReturnsFrenchTranslation()
{
    ServiceCollection services = new();
    services.AddGranitLocalization();
    services.Configure<GranitLocalizationOptions>(options =>
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

## Clés type-safe (source generator)

Le package `Granit.Localization.SourceGenerator` génère automatiquement des constantes
C# à partir des fichiers JSON de localisation, éliminant les chaînes magiques.

### Installation

```bash
dotnet add package Granit.Localization.SourceGenerator
```

### Configuration

Déclarer les fichiers JSON comme `AdditionalFiles` dans le `.csproj` du projet
consommateur :

```xml
<ItemGroup>
  <AdditionalFiles Include="Localization/**/*.json" />
</ItemGroup>
```

### Code généré

À partir du JSON suivant :

```json
{
  "culture": "fr",
  "texts": {
    "Granit:EntityNotFound": "L'entité est introuvable.",
    "Granit:Validation.Required": "Ce champ est obligatoire.",
    "Granit:Validation.MaxLength": "Maximum {0} caractères."
  }
}
```

Le source generator produit :

```csharp
public static class LocalizationKeys
{
    public static class Granit
    {
        public const string EntityNotFound = "Granit:EntityNotFound";

        public static class Validation
        {
            public const string Required = "Granit:Validation.Required";
            public const string MaxLength = "Granit:Validation.MaxLength";
        }
    }
}
```

### Utilisation

```csharp
// Avant — chaîne magique
string message = localizer["Granit:EntityNotFound"];

// Après — constante type-safe avec autocomplétion
string message = localizer[LocalizationKeys.Granit.EntityNotFound];
```

### Règles de génération

| Convention JSON | Résultat C# |
| --- | --- |
| Séparateur `:` (ex. `Granit:Key`) | Classe imbriquée + constante |
| Séparateur `.` (ex. `Validation.Required`) | Classes imbriquées |
| Objets JSON imbriqués | Aplatis avec `.` comme séparateur |
| Caractères invalides (`-`, espaces) | Remplacés par `_` |
| Identifiant commençant par un chiffre | Préfixé par `_` |

Le namespace de la classe générée correspond au `RootNamespace` du projet consommateur.

## Bonnes pratiques

1. **Un marker par module** — créer une classe marker par package/module pour isoler
   les espaces de noms des clés
2. **Préfixer les clés** — utiliser un préfixe cohérent pour éviter les collisions
   (ex. `"Granit:EntityNotFound"`, `"Patient:NotFound"`)
3. **Hériter `GranitLocalizationResource`** — pour bénéficier des messages d'erreur
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
| `Microsoft.Extensions.Options` | `IOptions<GranitLocalizationOptions>` |
| `SmartFormat` | Moteur de formatage avec pluralisation CLDR (remplace `string.Format`) |
| `Granit.Core` | Système de modules (`GranitModule`, `[DependsOn]`) |
| `Granit.Localization.SourceGenerator` | Source generator Roslyn pour constantes type-safe |
