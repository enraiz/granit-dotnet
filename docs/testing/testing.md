# Tests

Ce guide décrit l'infrastructure, les conventions et les patterns de tests utilisés
dans les packages Digital Dynamics Foundation.

Inspiré du guide [`Testing`](https://abp.io/docs/latest/testing/overall) d'ABP Framework.

## Philosophie

Digital Dynamics Foundation privilégie une **approche mixte** : écrire des tests
unitaires ou d'intégration là où c'est le plus efficace à écrire et à maintenir.

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
| [FluentAssertions](https://fluentassertions.com/) | Assertions lisibles | 8.* |
| [NSubstitute](https://nsubstitute.github.io/) | Mocking/stubbing | 5.* |
| [Bogus](https://github.com/bchavez/Bogus) | Génération de données de test | 35.* |
| [coverlet](https://github.com/coverlet-coverage/coverlet) | Couverture de code | 6.* |
| `Microsoft.EntityFrameworkCore.InMemory` | Base de données in-memory pour EF Core | 10.* |
| `Microsoft.Extensions.TimeProvider.Testing` | `FakeTimeProvider` pour les tests temporels | 10.* |

Les versions sont centralisées dans [Directory.Packages.props](../../Directory.Packages.props)
(Central Package Management).

## Structure des projets de tests

Chaque package Foundation a un projet de tests dédié, nommé `*.Tests` :

```text
src/
├── DigitalDynamics.Foundation.Core/
├── DigitalDynamics.Foundation.Guids/
├── DigitalDynamics.Foundation.Observability/
├── DigitalDynamics.Foundation.Persistence/
├── DigitalDynamics.Foundation.Security/
├── DigitalDynamics.Foundation.Timing/
└── DigitalDynamics.Foundation.Vault/

tests/
├── DigitalDynamics.Foundation.Core.Tests/
├── DigitalDynamics.Foundation.Guids.Tests/
├── DigitalDynamics.Foundation.Observability.Tests/
├── DigitalDynamics.Foundation.Persistence.Tests/
├── DigitalDynamics.Foundation.Security.Tests/
├── DigitalDynamics.Foundation.Timing.Tests/
└── DigitalDynamics.Foundation.Vault.Tests/
```

Chaque projet de tests référence uniquement le projet source correspondant. Pas de
base classes partagées : chaque classe de test est **autonome et scellée** (`sealed`).

## Exécution des tests

```bash
# Exécuter tous les tests
dotnet test

# Exécuter les tests d'un package spécifique
dotnet test tests/DigitalDynamics.Foundation.Timing.Tests

# Avec couverture de code (coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

## Conventions

### Nommage

Les classes de tests suivent le pattern `{ClasseTestée}Tests` :

| Classe source | Classe de test |
| --- | --- |
| `Clock` | `ClockTests` |
| `SequentialGuidGenerator` | `SequentialGuidGeneratorTests` |
| `AuditedEntityInterceptor` | `AuditedEntityInterceptorTests` |
| `CurrentUserService` | `CurrentUserServiceTests` |

Les méthodes de test suivent le pattern `Method_Scenario_ExpectedBehavior` :

```csharp
[Fact]
public void Now_ReturnsUtcFromTimeProvider() { ... }

[Fact]
public async Task SaveChangesAsync_OnAdd_SetsCreatedFields() { ... }

[Fact]
public void UserId_WithAuthenticatedUser_ReturnsSubClaim() { ... }
```

### Pattern AAA (Arrange-Act-Assert)

Tous les tests suivent strictement le pattern AAA :

```csharp
[Fact]
public void Normalize_ConvertsLocalOffsetToUtc()
{
    // Arrange - DateTimeOffset avec offset +02:00 (Europe/Brussels en été)
    var localTime = new DateTimeOffset(2026, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));

    // Act
    var normalized = _clock.Normalize(localTime);

    // Assert - Doit être converti en UTC (+00:00), même instant
    normalized.Offset.Should().Be(TimeSpan.Zero);
    normalized.Should().Be(new DateTimeOffset(2026, 6, 15, 12, 30, 0, TimeSpan.Zero));
}
```

### Classes de test scellées

Chaque classe de test est `public sealed class`. Pas de hiérarchie d'héritage, pas de
base class partagée. Les dépendances communes sont initialisées dans le constructeur :

```csharp
public sealed class ClockTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly ICurrentTimezoneProvider _timezoneProvider;
    private readonly Clock _clock;

    public ClockTests()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _timezoneProvider = Substitute.For<ICurrentTimezoneProvider>();
        _clock = new Clock(_fakeTimeProvider, _timezoneProvider);
    }

    // ... tests
}
```

### Headers descriptifs

Chaque fichier de test commence par un header qui décrit ce qui est testé et
l'approche utilisée :

```csharp
// =============================================================================
// Tests - AuditedEntityInterceptor
// =============================================================================
// Vérifie que les champs d'audit HDS sont correctement remplis
// lors de la création et modification des entités.
//
// Approche : on enregistre l'intercepteur dans le DbContext et on appelle
// SaveChangesAsync directement, ce qui déclenche l'intercepteur naturellement.
// IClock est mocké pour des assertions exactes (pas de BeCloseTo).
// =============================================================================
```

## Mocking avec NSubstitute

### Mocker une interface

```csharp
var clock = Substitute.For<IClock>();
var fixedNow = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
clock.Now.Returns(fixedNow);
```

### Mocker IOptions

```csharp
var options = Substitute.For<IOptions<GuidGeneratorOptions>>();
options.Value.Returns(new GuidGeneratorOptions
{
    DefaultSequentialGuidType = SequentialGuidType.SequentialAsString
});
```

Ou avec le helper `Options.Create()` pour les cas simples :

```csharp
var options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
{
    TransitMountPoint = "transit"
});
```

### Mocker IHttpContextAccessor (Security)

Pattern spécifique pour les tests de sécurité qui nécessitent un `ClaimsPrincipal` :

```csharp
private static CurrentUserService CreateService(params Claim[] claims)
{
    var identity = new ClaimsIdentity(claims, "Bearer");
    var principal = new ClaimsPrincipal(identity);

    var httpContext = new DefaultHttpContext { User = principal };
    var accessor = Substitute.For<IHttpContextAccessor>();
    accessor.HttpContext.Returns(httpContext);

    return new CurrentUserService(accessor);
}
```

### Quand mocker, quand utiliser l'implémentation réelle

| Dépendance | Stratégie | Raison |
| --- | --- | --- |
| `IClock` | Mock (`Substitute.For<IClock>()`) | Assertions temporelles déterministes |
| `IGuidGenerator` | Mock | Contrôle des identifiants générés |
| `ICurrentUserService` | Mock | Simuler différents contextes utilisateur |
| `TimeProvider` | `FakeTimeProvider` (implémentation réelle de test) | Fourni par Microsoft pour les tests temporels |
| `DbContext` | In-memory (implémentation réelle) | Tester les intercepteurs EF Core avec un vrai pipeline |
| `IVaultClient` | Mock | Pas de Vault en test unitaire |

## Tests temporels avec FakeTimeProvider

`FakeTimeProvider` (du package `Microsoft.Extensions.TimeProvider.Testing`) permet de
contrôler le temps dans les tests sans mocker `IClock` directement. Utile quand on
teste l'implémentation `Clock` elle-même :

```csharp
var fakeTimeProvider = new FakeTimeProvider();
var timezoneProvider = Substitute.For<ICurrentTimezoneProvider>();
var clock = new Clock(fakeTimeProvider, timezoneProvider);

// Figer le temps à un instant précis
var fixedTime = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
fakeTimeProvider.SetUtcNow(fixedTime);

clock.Now.Should().Be(fixedTime);
```

> **Toujours utiliser des dates fixes dans les tests.** Ne jamais utiliser
> `DateTimeOffset.UtcNow` dans les assertions : cela introduit du non-déterminisme
> et oblige à utiliser `BeCloseTo` avec une tolérance arbitraire.

### Avant / après : assertions temporelles

```csharp
// Avant (fragile, tolérance 5s, faux positifs possibles)
entity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

// Après (déterministe, assertion exacte)
entity.CreatedAt.Should().Be(fixedNow);
```

## Tests EF Core avec base in-memory

Les tests d'intercepteurs EF Core utilisent `UseInMemoryDatabase` avec un nom unique
par test pour garantir l'isolation :

```csharp
private TestDbContext CreateContext()
{
    var interceptor = new AuditedEntityInterceptor(
        _currentUserService, _clock, _guidGenerator);
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .AddInterceptors(interceptor)
        .Options;
    return new TestDbContext(options);
}
```

### Entités de test internes

Chaque classe de test définit ses propres entités et `DbContext` comme classes
internes `private sealed class`. Cela évite les dépendances entre tests et rend
chaque fichier de test autosuffisant :

```csharp
private sealed class TestEntity : AuditedEntity
{
    public string Name { get; set; } = string.Empty;
}

private sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>()
            .Property(e => e.Id).ValueGeneratedNever();
    }
}
```

> `ValueGeneratedNever()` est nécessaire car l'intercepteur `AuditedEntityInterceptor`
> gère lui-même la génération des GUID via `IGuidGenerator`.

## Tests de conformité HDS / RGPD

Certains tests vérifient directement des exigences réglementaires :

### Audit trail HDS

Les tests de `AuditedEntityInterceptor` vérifient que les champs d'audit sont
correctement remplis, conformément à l'exigence HDS de traçabilité sur 3 ans :

```csharp
[Fact]
public async Task SaveChangesAsync_OnAdd_SetsCreatedFields()
{
    // Arrange
    await using var context = CreateContext();
    var entity = new TestEntity { Name = "Test" };
    context.TestEntities.Add(entity);

    // Act
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Assert
    entity.CreatedAt.Should().Be(FixedNow);
    entity.CreatedBy.Should().Be("user-test-123");
    entity.Id.Should().Be(FixedGuid);
}
```

### Soft delete RGPD

Les tests de `SoftDeleteInterceptor` vérifient que la suppression physique est
convertie en suppression logique, avec capture de `DeletedAt` et `DeletedBy` :

```csharp
[Fact]
public async Task SaveChangesAsync_OnDelete_ConvertToSoftDelete()
{
    // Arrange
    await using var context = CreateContext();
    var entity = new TestSoftDeletableEntity { /* ... */ };
    context.Entities.Add(entity);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Supprimer l'entité
    context.Entities.Remove(entity);

    // Act
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Assert — l'entité est soft-deleted (pas physiquement supprimée)
    entity.IsDeleted.Should().BeTrue();
    entity.DeletedAt.Should().Be(FixedNow);
    entity.DeletedBy.Should().Be("user-test-123");
}
```

### UTC uniquement

Les tests de `Clock` vérifient que l'horloge retourne toujours UTC (conformité HDS) :

```csharp
[Fact]
public void Now_IsAlwaysUtc()
{
    var now = _clock.Now;
    now.Offset.Should().Be(TimeSpan.Zero,
        "le Clock doit toujours retourner UTC (conformité HDS)");
}
```

## Assertions avec FluentAssertions

FluentAssertions est la bibliothèque d'assertions standard. Exemples de patterns
fréquents :

```csharp
// Valeur exacte
entity.CreatedAt.Should().Be(fixedNow);

// Non vide
guid.Should().NotBe(Guid.Empty);

// Collection
sut.Roles.Should().BeEquivalentTo(new[] { "admin", "practitioner" });

// Ordonnancement
guids.Should().BeInAscendingOrder();

// Booléen
sut.IsAuthenticated.Should().BeTrue();

// Null
sut.UserId.Should().BeNull();

// Count
guids.Should().HaveCount(10_000, "tous les GUID doivent être uniques");

// Message contextuel (raison)
now.Offset.Should().Be(TimeSpan.Zero,
    "le Clock doit toujours retourner UTC (conformité HDS)");
```

> Toujours ajouter un message contextuel (`because`) pour les assertions dont
> l'échec ne serait pas immédiatement compréhensible.

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
    <PackageReference Include="FluentAssertions" />
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

| Package additionnel | Projet de tests | Usage |
| --- | --- | --- |
| `Microsoft.EntityFrameworkCore.InMemory` | Persistence.Tests | DbContext in-memory |
| `Microsoft.Extensions.TimeProvider.Testing` | Timing.Tests | `FakeTimeProvider` |
| `Microsoft.Extensions.Configuration.Binder` | Observability.Tests | Binding d'options |

## Bonnes pratiques

1. **Un projet de tests par package** — chaque package Foundation a son projet
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
