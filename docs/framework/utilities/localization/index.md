# Localisation

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
│   └── LocalizationServiceCollectionExtensions.cs  (AddGranitLocalization)
├── EmbeddedJsonSource.cs                      (Assembly + préfixe de ressource)
├── GranitLocalizationModule.cs                (module Granit)
├── GranitLocalizationOptions.cs               (options de configuration)
├── GranitLocalizationResource.cs              (marker — ressource de base)
├── LanguageInfo.cs                            (info langue pour UI)
├── LocalizationResourceInfo.cs               (info ressource + API fluent)
├── LocalizationResourceStore.cs              (registre des ressources)
└── Localization/
    └── Granit/
        ├── fr.json
        └── en.json
```

## Services enregistrés

| Service | Implémentation | Lifetime | Notes |
| --- | --- | --- | --- |
| `IStringLocalizerFactory` | `JsonStringLocalizerFactory` | Singleton | Cache par type via `ConcurrentDictionary<Type, Lazy<IStringLocalizer>>` |
| `IStringLocalizer<T>` | `StringLocalizer<T>` | Transient | Délègue à `IStringLocalizerFactory` |

Tous les enregistrements utilisent `TryAdd*` pour permettre le remplacement dans les tests.

## Pages

| Page | Description |
| --- | --- |
| [configuration.md](configuration.md) | Installation, ressources JSON, classes marker, options |
| [localizer.md](localizer.md) | IStringLocalizer, culture fallback, tests, bonnes pratiques |
| [endpoints.md](endpoints.md) | Endpoint HTTP `GET /api/granit/localization` pour clients SPA |
