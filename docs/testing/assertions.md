# Assertions et contrôle du temps

[← Index des tests](index.md)

Ce guide couvre les patterns d'assertions avec FluentAssertions et le contrôle
déterministe du temps avec `FakeTimeProvider`.

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

## Contrôle du temps avec FakeTimeProvider

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
