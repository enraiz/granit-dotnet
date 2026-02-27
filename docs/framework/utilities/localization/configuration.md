# Configuration — Granit.Localization

## Installation

```bash
dotnet add package Granit.Localization
```

## Enregistrement

### Avec le système de modules (recommandé)

Ajouter `[DependsOn(typeof(GranitLocalizationModule))]` sur le module applicatif :

```csharp
[DependsOn(typeof(GranitLocalizationModule))]
public sealed class MyAppModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<GranitLocalizationOptions>(options =>
        {
            options.Resources
                .Add<MyAppResource>(defaultCulture: "fr")
                .AddJson(
                    typeof(MyAppModule).Assembly,
                    "MyApp.Localization.MyApp")
                .AddBaseTypes(typeof(GranitLocalizationResource));

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
builder.Services.AddGranitLocalization();
```

La configuration des ressources se fait dans le module ou via `IConfigureOptions<GranitLocalizationOptions>` :

```csharp
services.Configure<GranitLocalizationOptions>(options =>
{
    options.Resources
        .Add<MyAppResource>(defaultCulture: "fr")
        .AddJson(typeof(Program).Assembly, "MyApp.Localization.MyApp")
        .AddBaseTypes(typeof(GranitLocalizationResource));
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

> **Important** : ne pas nommer les fichiers `{Resource}.{culture}.json`
> (ex. `MyApp.fr.json`). MSBuild détecte le suffixe de culture et attribue le même
> nom de ressource embarquée aux deux fichiers, provoquant une collision silencieuse.
> Utiliser systématiquement la structure `{Resource}/{culture}.json`.

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
[InheritResource(typeof(GranitLocalizationResource))]
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
    .AddBaseTypes(typeof(GranitLocalizationResource));
```

Les deux mécanismes peuvent être combinés. Les clés sont résolues dans cet ordre :
ressource courante → ressources parentes (dans l'ordre de déclaration).

## GranitLocalizationOptions

```csharp
public sealed class GranitLocalizationOptions
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

## Ressource Granit intégrée

`GranitLocalizationResource` est la ressource de base fournie par ce package.
Elle contient les messages d'erreur communs à tous les modules :

| Clé | FR | EN |
| --- | --- | --- |
| `Granit:EntityNotFound` | L'entité de type {0} avec l'identifiant {1} n'a pas été trouvée. | Entity of type {0} with identifier {1} was not found. |
| `Granit:ValidationError` | Erreur de validation. | Validation error. |
| `Granit:Unauthorized` | Accès non autorisé. | Unauthorized access. |
| `Granit:Forbidden` | Accès interdit. | Forbidden. |
| `Granit:InternalError` | Une erreur interne est survenue. | An internal error occurred. |

Les modules applicatifs peuvent hériter de `GranitLocalizationResource` pour
réutiliser ces messages sans les redéfinir.
