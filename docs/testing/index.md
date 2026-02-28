# Tests

Ce guide décrit l'infrastructure, les conventions et les patterns de tests utilisés
dans les packages Granit.

Inspiré du guide [`Testing`](https://abp.io/docs/latest/testing/overall) d'ABP Framework.

## Philosophie

Granit privilégie une **approche mixte** : écrire des tests unitaires ou d'intégration
là où c'est le plus efficace à écrire et à maintenir.

- **Tests unitaires** : testent une classe isolée en mockant ses dépendances.
  Rapides à exécuter, mais nécessitent la gestion des mocks
- **Tests d'intégration** : testent un service avec une infrastructure réelle
  (DbContext in-memory, intercepteurs). Plus lents mais plus réalistes, et souvent
  plus simples à écrire car ils ne nécessitent pas de mocker chaque dépendance

> Les tests font partie de la **Definition of Done** de chaque story.
> Un package sans tests n'est pas livrable.

## Stack de tests

| Package | Rôle | Version |
| --- | --- | --- |
| [xUnit v3](https://xunit.net/) | Framework de tests | 3.* |
| [Shouldly](https://docs.shouldly.org/) | Assertions lisibles | 4.* |
| [NSubstitute](https://nsubstitute.github.io/) | Mocking/stubbing | 5.* |
| [Bogus](https://github.com/bchavez/Bogus) | Génération de données de test | 35.* |
| [coverlet](https://github.com/coverlet-coverage/coverlet) | Couverture de code | 6.* |
| `Microsoft.EntityFrameworkCore.InMemory` | Base de données in-memory pour EF Core | 10.* |
| `Microsoft.Extensions.TimeProvider.Testing` | `FakeTimeProvider` pour les tests temporels | 10.* |

Les versions sont centralisées dans [Directory.Packages.props](../../Directory.Packages.props)
(Central Package Management).

## Structure des projets de tests

Chaque package Granit a un projet de tests dédié, nommé `*.Tests` :

```text
src/
├── Granit.ApiDocumentation/
├── Granit.ApiVersioning/
├── Granit.Authentication.JwtBearer/
├── Granit.Authentication.Keycloak/
├── Granit.Authorization/
├── Granit.Authorization.EntityFrameworkCore/
├── Granit.Caching/
├── Granit.Caching.Hybrid/
├── Granit.Caching.StackExchangeRedis/
├── Granit.Core/
├── Granit.Diagnostics/
├── Granit.Encryption/
├── Granit.ExceptionHandling/
├── Granit.Guids/
├── Granit.Idempotency/
├── Granit.Localization/
├── Granit.MultiTenancy/
├── Granit.Observability/
├── Granit.Persistence/
├── Granit.Security/
├── Granit.Settings/
├── Granit.Timing/
├── Granit.Vault/
├── Granit.Wolverine/
└── Granit.Wolverine.Postgresql/

tests/
├── Granit.ApiDocumentation.Tests/
├── Granit.ApiVersioning.Tests/
├── Granit.Authentication.JwtBearer.Tests/
├── Granit.Authentication.Keycloak.Tests/
├── Granit.Authorization.EntityFrameworkCore.Tests/
├── Granit.Authorization.Tests/
├── Granit.Caching.Hybrid.Tests/
├── Granit.Caching.StackExchangeRedis.Tests/
├── Granit.Caching.Tests/
├── Granit.Core.Tests/
├── Granit.Diagnostics.Tests/
├── Granit.Encryption.Tests/
├── Granit.ExceptionHandling.Tests/
├── Granit.Guids.Tests/
├── Granit.Idempotency.Tests/
├── Granit.Localization.Tests/
├── Granit.MultiTenancy.Tests/
├── Granit.Observability.Tests/
├── Granit.Persistence.Tests/
├── Granit.Security.Tests/
├── Granit.Settings.Tests/
├── Granit.Timing.Tests/
├── Granit.Vault.Tests/
├── Granit.Wolverine.Postgresql.Tests/
└── Granit.Wolverine.Tests/
```

Chaque projet de tests référence uniquement le projet source correspondant. Pas de
base classes partagées : chaque classe de test est **autonome et scellée** (`sealed`).

## Exécution des tests

```bash
# Exécuter tous les tests
dotnet test

# Exécuter les tests d'un package spécifique
dotnet test tests/Granit.Timing.Tests

# Avec couverture de code (coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

## Structure d'un projet de tests (csproj)

Chaque projet de tests suit la même structure standardisée :

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Bogus" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\{Package}\{Package}.csproj" />
  </ItemGroup>
</Project>
```

Les versions sont omises car gérées par Central Package Management
(`Directory.Packages.props`).

Des packages spécifiques sont ajoutés selon les besoins :

| Package additionnel | Projets de tests | Usage |
| --- | --- | --- |
| `Microsoft.EntityFrameworkCore.InMemory` | Persistence.Tests, Authorization.EntityFrameworkCore.Tests | DbContext in-memory |
| `Microsoft.Extensions.TimeProvider.Testing` | Timing.Tests | `FakeTimeProvider` |
| `Microsoft.AspNetCore.Mvc.Testing` | ApiDocumentation.Tests, ApiVersioning.Tests, Diagnostics.Tests, Idempotency.Tests | Tests d'intégration ASP.NET Core |
| `Microsoft.AspNetCore.App` (FrameworkReference) | Authentication.JwtBearer.Tests, Authentication.Keycloak.Tests, Core.Tests, ExceptionHandling.Tests, Idempotency.Tests, MultiTenancy.Tests, Observability.Tests, Security.Tests, Settings.Tests, Wolverine.Tests | Accès aux types ASP.NET Core |

## Guides thématiques

| Guide | Contenu |
| --- | --- |
| [Conventions](conventions.md) | Nommage, pattern AAA, classes scellées, headers descriptifs |
| [Mocking](mocking.md) | NSubstitute : interfaces, IOptions, IHttpContextAccessor, stratégie |
| [Assertions et temps](assertions.md) | Shouldly, FakeTimeProvider, déterminisme temporel |
| [Tests d'intégration EF Core](integration.md) | DbContext in-memory, entités de test internes |
| [Conformité HDS / RGPD](hds-rgpd.md) | Audit trail, soft delete, UTC |

## Bonnes pratiques

1. **Un projet de tests par package** — chaque package Granit a son projet
   `*.Tests` correspondant, pas de projet de tests partagé
2. **Classes scellées, pas d'héritage** — chaque classe de test est `sealed` et
   autonome, sans base class partagée
3. **Assertions exactes, jamais approximatives** — utiliser des valeurs fixes pour
   le temps et les identifiants, jamais `BeCloseTo`
4. **Isolation par test** — chaque test crée son propre `DbContext` avec un nom de
   base de données unique (`Guid.NewGuid().ToString()`)
5. **Entités de test internes** — les entités et `DbContext` de test sont des classes
   internes `private sealed class` dans la classe de test
6. **Mocker au bon niveau** — mocker les interfaces (`IClock`, `IGuidGenerator`) dans
   les tests applicatifs, utiliser les implémentations de test (`FakeTimeProvider`)
   quand on teste l'infrastructure elle-même
7. **Headers descriptifs** — chaque fichier de test commence par un header décrivant
   ce qui est testé et l'approche
8. **Exécution parallèle** — les tests sont conçus pour s'exécuter en parallèle
   (pas d'état partagé mutable entre tests)
9. **`CancellationToken` via `TestContext`** — utiliser
   `TestContext.Current.CancellationToken` (xUnit v3) pour propager l'annulation
   dans les tests async
