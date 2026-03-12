# Mocking avec NSubstitute

[← Index des tests](index.md)

NSubstitute est la bibliothèque de mocking standard dans Granit. Ce guide couvre
les patterns les plus fréquents et la stratégie de mocking à adopter.

## Mocker une interface

```csharp
var clock = Substitute.For<IClock>();
var fixedNow = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
clock.Now.Returns(fixedNow);
```

## Mocker IOptions

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

## Mocker IHttpContextAccessor

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

## Stratégie : quand mocker, quand utiliser l'implémentation réelle

| Dépendance | Stratégie | Raison |
| --- | --- | --- |
| `IClock` | Mock (`Substitute.For<IClock>()`) | Assertions temporelles déterministes |
| `IGuidGenerator` | Mock | Contrôle des identifiants générés |
| `ICurrentUserService` | Mock | Simuler différents contextes utilisateur |
| `TimeProvider` | `FakeTimeProvider` (implémentation réelle de test) | Fourni par Microsoft pour les tests temporels |
| `DbContext` | In-memory (implémentation réelle) | Tester les intercepteurs EF Core avec un vrai pipeline |
| `IVaultClient` | Mock | Pas de Vault en test unitaire |
