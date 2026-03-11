# Conventions de tests

[← Index des tests](index.md)

Ce guide décrit les conventions d'écriture des tests dans les packages Granit :
nommage, structure AAA, classes scellées et headers de fichiers.

## Nommage

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

## Pattern AAA (Arrange-Act-Assert)

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
    normalized.Offset.ShouldBe(TimeSpan.Zero);
    normalized.ShouldBe(new DateTimeOffset(2026, 6, 15, 12, 30, 0, TimeSpan.Zero));
}
```

## Classes scellées

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

## Headers descriptifs

Chaque fichier de test commence par un header qui décrit ce qui est testé et
l'approche utilisée :

```csharp
// =============================================================================
// Tests - AuditedEntityInterceptor
// =============================================================================
// Vérifie que les champs d'audit ISO 27001 sont correctement remplis
// lors de la création et modification des entités.
//
// Approche : on enregistre l'intercepteur dans le DbContext et on appelle
// SaveChangesAsync directement, ce qui déclenche l'intercepteur naturellement.
// IClock est mocké pour des assertions exactes .
// =============================================================================
```
