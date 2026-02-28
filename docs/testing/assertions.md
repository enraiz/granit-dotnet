# Assertions et contrôle du temps

[← Index des tests](index.md)

Ce guide couvre les patterns d'assertions avec Shouldly et le contrôle
déterministe du temps avec `FakeTimeProvider`.

## Assertions avec Shouldly

Shouldly est la bibliothèque d'assertions standard. Exemples de patterns
fréquents :

```csharp
// Valeur exacte
entity.CreatedAt.ShouldBe(fixedNow);

// Non vide
guid.ShouldNotBe(Guid.Empty);

// Collection
sut.Roles.ShouldBe(new[] { "admin", "practitioner" });

// Ordonnancement
guids.ShouldBeInOrder(SortDirection.Ascending);

// Booléen
sut.IsAuthenticated.ShouldBeTrue();

// Null
sut.UserId.ShouldBeNull();

// Count
guids.Count().ShouldBe(10_000, "tous les GUID doivent être uniques");

// Message contextuel (raison)
now.Offset.ShouldBe(TimeSpan.Zero,
    "le Clock doit toujours retourner UTC (conformité HDS)");
```

> Toujours ajouter un message contextuel (`customMessage`) pour les assertions
> dont l'échec ne serait pas immédiatement compréhensible.

### Exceptions

```csharp
// Sync
InvalidOperationException ex = Should.Throw<InvalidOperationException>(act);
ex.Message.ShouldContain("expected text");

// Async
InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(act);
ex.Message.ShouldContain("expected text");

// Pas d'exception
Should.NotThrow(act);
await Should.NotThrowAsync(asyncAct);
```

### Correspondance FluentAssertions → Shouldly

| FluentAssertions | Shouldly |
| --- | --- |
| `.Should().Be(x)` | `.ShouldBe(x)` |
| `.Should().NotBe(x)` | `.ShouldNotBe(x)` |
| `.Should().BeNull()` | `.ShouldBeNull()` |
| `.Should().NotBeNull()` | `.ShouldNotBeNull()` |
| `.Should().BeTrue()` | `.ShouldBeTrue()` |
| `.Should().BeFalse()` | `.ShouldBeFalse()` |
| `.Should().BeEmpty()` | `.ShouldBeEmpty()` |
| `.Should().Contain(x)` | `.ShouldContain(x)` |
| `.Should().HaveCount(n)` | `.Count().ShouldBe(n)` |
| `.Should().BeOfType<T>()` | `.ShouldBeOfType<T>()` |
| `.Should().BeEquivalentTo(x)` | `.ShouldBe(x)` |
| `.Should().Throw<T>()` | `Should.Throw<T>(act)` |
| `.Should().ThrowAsync<T>()` | `await Should.ThrowAsync<T>(act)` |

## Contrôle du temps avec FakeTimeProvider

`FakeTimeProvider` (du package `Microsoft.Extensions.TimeProvider.Testing`) permet de
contrôler le temps dans les tests sans mocker `IClock` directement. Utile quand on
teste l'implémentation `Clock` elle-même :

```csharp
FakeTimeProvider fakeTimeProvider = new();
ICurrentTimezoneProvider timezoneProvider = Substitute.For<ICurrentTimezoneProvider>();
Clock clock = new(fakeTimeProvider, timezoneProvider);

// Figer le temps à un instant précis
DateTimeOffset fixedTime = new(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
fakeTimeProvider.SetUtcNow(fixedTime);

clock.Now.ShouldBe(fixedTime);
```

> **Toujours utiliser des dates fixes dans les tests.** Ne jamais utiliser
> `DateTimeOffset.UtcNow` dans les assertions : cela introduit du non-déterminisme
> et oblige à utiliser des comparaisons approximatives.

### Avant / après : assertions temporelles

```csharp
// Avant (fragile, tolérance 5s, faux positifs possibles)
(entity.CreatedAt - DateTimeOffset.UtcNow).Duration()
    .ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(5));

// Après (déterministe, assertion exacte)
entity.CreatedAt.ShouldBe(fixedNow);
```
