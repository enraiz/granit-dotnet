# Structure des modules (Blueprint)

[← Architecture](architecture.md) | [← Conventions](../index.md)

Chaque package Granit suit une arborescence prévisible. Un développeur qui ouvre
`Granit.Identity` doit y trouver la même structure que dans `Granit.Workflow` ou
`Granit.DataExchange`. Ce document définit le blueprint standard.

## Principe directeur

> **Prédictibilité > Liberté.**
> Un nouveau fichier doit pouvoir être classé sans hésitation.
> Si le dossier n'existe pas encore dans le module, le créer en suivant ce blueprint.

## Blueprint d'un package métier

```text
Granit.Example/
├── Domain/                    Entités, agrégats, Value Objects, enums domaine
├── Events/                    Messages Wolverine (events, commands)
├── Exceptions/                Exceptions métier (héritent de BusinessException, etc.)
├── Extensions/                Méthodes d'extension (DI, builders, etc.)
├── Handlers/                  Handlers Wolverine (consommation d'events/commands)
├── Internal/                  Implémentations internes (services, stores, etc.)
├── Localization/              Fichiers JSON de traduction (9 cultures)
├── Options/                   Classes de configuration IOptions<T>
├── GranitExampleModule.cs     Classe de module (racine obligatoire)
├── IExampleReader.cs          Interfaces publiques (racine du module)
├── IExampleWriter.cs
├── Granit.Example.csproj
└── README.md
```

## Blueprint d'un package `*.Endpoints`

```text
Granit.Example.Endpoints/
├── Dtos/                      Request/Response records (contrats API)
├── Endpoints/                 Classes Minimal API (MapGet, MapPost, etc.)
├── Extensions/                EndpointRouteBuilderExtensions
├── Internal/                  Implémentations internes (LocalizationResource, SchemaExampleProvider)
├── Localization/              Fichiers JSON de traduction
├── Options/                   Configuration des endpoints
├── Permissions/               PermissionDefinitionProvider
├── Validators/                FluentValidation validators
├── GranitExampleEndpointsModule.cs
├── Granit.Example.Endpoints.csproj
└── README.md
```

## Blueprint d'un package `*.EntityFrameworkCore`

```text
Granit.Example.EntityFrameworkCore/
├── Configurations/            Fluent API (IEntityTypeConfiguration<T>)
├── Entities/                  Entités EF-only (pas dans le package métier)
├── Extensions/                ServiceCollectionExtensions (AddDbContextFactory, etc.)
├── Internal/                  DbContext, EfCoreStore implémentations
├── GranitExampleEntityFrameworkCoreModule.cs
├── IExampleDbContext.cs       Interface host (optionnelle, pour les DbContext partagés)
├── Granit.Example.EntityFrameworkCore.csproj
└── README.md
```

## Blueprint d'un package `*.Wolverine`

```text
Granit.Example.Wolverine/
├── Extensions/                ServiceCollectionExtensions
├── Internal/                  Handlers, dispatchers, publishers
├── GranitExampleWolverineModule.cs
├── Granit.Example.Wolverine.csproj
└── README.md
```

## Référentiel des dossiers standard

Les dossiers ci-dessous sont les seuls noms reconnus. Tout autre nom est considéré
comme spécifique au module et doit être justifié.

### Dossiers universels (tous packages)

| Dossier | Contenu | Visibilité attendue |
| --- | --- | --- |
| `Extensions/` | Méthodes d'extension (`IServiceCollection`, `IApplicationBuilder`, `ModelBuilder`) | `public static` |
| `Internal/` | Implémentations concrètes, services, stores, handlers, DbContext | `internal sealed` |
| `Options/` | Classes de configuration liées à `IOptions<T>` / `IOptionsMonitor<T>` | `public` |
| `Localization/` | Fichiers JSON de traduction (`en.json`, `fr.json`, etc.) | — |

### Dossiers domaine (packages métier)

| Dossier | Contenu | Visibilité attendue |
| --- | --- | --- |
| `Domain/` | Entités riches, agrégats, Value Objects, enums domaine | `public` |
| `Events/` | Messages Wolverine (events d'intégration, commands) | `public sealed record` |
| `Exceptions/` | Exceptions métier (`BusinessException`, `NotFoundException`, etc.) | `public sealed` |
| `Handlers/` | Handlers Wolverine pour events/commands | `internal sealed` |

### Dossiers API (packages `*.Endpoints`)

| Dossier | Contenu | Visibilité attendue |
| --- | --- | --- |
| `Dtos/` | Records de requête (`*Request`) et réponse (`*Response`) | `public sealed record` |
| `Endpoints/` | Classes Minimal API qui déclarent les routes | `internal sealed` |
| `Permissions/` | `*PermissionDefinitionProvider` | `internal sealed` |
| `Validators/` | Validateurs FluentValidation (`*Validator`) | `internal sealed` |

### Dossiers persistance (packages `*.EntityFrameworkCore`)

| Dossier | Contenu | Visibilité attendue |
| --- | --- | --- |
| `Configurations/` | `IEntityTypeConfiguration<T>` (Fluent API) | `internal sealed` |
| `Entities/` | Entités EF-only qui n'existent pas dans le package métier | `public sealed` |

## Règles de placement

### Où mettre quoi ?

| Type de fichier | Emplacement | Exemple |
| --- | --- | --- |
| Interface publique | **Racine du module** | `IBlobStorage.cs` |
| Record public (contrat de service) | **Racine du module** | `BlobUploadRequest.cs` |
| Classe de module | **Racine du module** | `GranitBlobStorageModule.cs` |
| Entité domaine | **`Domain/`** | `Domain/ExportJob.cs` |
| Exception métier | **`Exceptions/`** | `Exceptions/BlobNotFoundException.cs` |
| Event Wolverine | **`Events/`** | `Events/BlobDeleted.cs` |
| Implémentation `internal` | **`Internal/`** | `Internal/DefaultBlobStorage.cs` |
| Configuration `IOptions<T>` | **`Options/`** | `Options/BlobStorageOptions.cs` |
| Extension DI | **`Extensions/`** | `Extensions/BlobStorageServiceCollectionExtensions.cs` |

### Règle d'or pour `Internal/`

Tout type déclaré `internal` doit résider dans `Internal/` ou dans un sous-dossier
thématique du module (`Validators/`, `Endpoints/`, `Configurations/`, etc.).
Un type `internal` **ne doit jamais** être à la racine du module.

> Cette règle est vérifiée par le test d'architecture
> `Internal_types_should_not_be_at_module_root` dans `Granit.ArchitectureTests`.

### Profondeur maximale

**2 niveaux maximum** sous le module. L'IDE gère la recherche — pas besoin
d'imbriquer les dossiers.

```text
✅  Granit.Foo/Internal/MyStore.cs
✅  Granit.Foo/Domain/MyEntity.cs
❌  Granit.Foo/Domain/SubDomain/Values/MyValue.cs   (trop profond)
```

Exception : les modules complexes multi-feature (ex. `Granit.DataExchange`) peuvent
utiliser une découpe par feature au premier niveau, puis les dossiers standard :

```text
Granit.DataExchange/
├── Export/
│   ├── Domain/
│   ├── Internal/
│   └── Messages/
├── Import/
│   ├── Domain/
│   ├── Internal/
│   ├── Pipeline/
│   └── Parsing/
└── GranitDataExchangeModule.cs
```

## Anti-patterns

| Pattern | Pourquoi c'est interdit | Alternative |
| --- | --- | --- |
| `Helpers/`, `Utils/`, `Common/` | Poubelle sans sémantique. Si la classe a une responsabilité claire, elle mérite un vrai dossier. | `Internal/` ou un dossier thématique |
| `Services/` | Trop générique — ne dit rien sur la responsabilité. | `Internal/` pour les implémentations, racine pour les interfaces |
| `Abstractions/` | Granit ne sépare pas les interfaces dans un package dédié. Les interfaces vivent à la racine du module. | Racine du module |
| `Models/` | Ambigu entre DTO, entité et View Model. | `Domain/` pour les entités, `Dtos/` pour les DTOs |
| `DbContext/` (dossier) | Pattern legacy. Le DbContext est un détail d'implémentation. | `Internal/` |
| `Stores/` (dossier séparé) | Les stores sont des implémentations internes. | `Internal/` |

## Synchronisation dossier / namespace

Le namespace doit **toujours** correspondre à la structure de dossiers :

```text
src/Granit.Foo/Internal/MyStore.cs  →  namespace Granit.Foo.Internal;
src/Granit.Foo/Domain/MyEntity.cs   →  namespace Granit.Foo.Domain;
src/Granit.Foo/IMyService.cs        →  namespace Granit.Foo;
```

> Vérifié par le test d'architecture `Namespace_should_match_folder_structure_in_src`.

## Tests d'architecture associés

| Test | Fichier | Ce qu'il vérifie |
| --- | --- | --- |
| `Internal_types_should_not_be_at_module_root` | `FileOrganizationTests.cs` | Types `internal` pas à la racine |
| `Domain_types_should_reside_in_Domain_folder` | `FileOrganizationTests.cs` | Entités domaine dans `Domain/` |
| `EfCore_domain_types_should_reside_in_Entities_folder` | `FileOrganizationTests.cs` | Entités EF dans `Entities/` |
| `Options_classes_should_reside_in_Options_folder` | `FileOrganizationTests.cs` | Options dans `Options/` |
| `Extension_methods_should_reside_in_Extensions_folder` | `FileOrganizationTests.cs` | Extensions dans `Extensions/` |
| `DbContext_classes_should_reside_in_Internal_folder` | `FileOrganizationTests.cs` | DbContext dans `Internal/` |
| `DbContext_classes_should_follow_canonical_pattern` | `SourceCodeAntiPatternTests.cs` | DbContext : `internal sealed`, primary constructor, `ApplyGranitConventions` |
| `Namespace_should_match_folder_structure_in_src` | `SourceCodeAntiPatternTests.cs` | Namespace = chemin de dossier |
| `Public_types_should_not_reside_in_Internal_namespaces` | `ClassDesignTests.cs` | Types `public` pas dans `Internal` |

## Checklist nouveau module

Avant de commencer un nouveau module :

1. Créer le projet avec `dotnet new classlib -n Granit.Example`
2. Ajouter `GranitExampleModule.cs` à la racine
3. Créer `Extensions/ExampleServiceCollectionExtensions.cs` avec `AddGranitExample()`
4. Créer `README.md` ([template](../../README-template.md))
5. Créer le projet de tests miroir `tests/Granit.Example.Tests/`
6. Ajouter les dossiers nécessaires selon le blueprint ci-dessus
7. Vérifier avec `dotnet test tests/Granit.ArchitectureTests` que la structure est conforme
