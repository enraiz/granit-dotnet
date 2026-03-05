# Identity Provider

Deux packages constituent la couche d'identité de Granit :
abstractions découplées d'un côté, implémentation Keycloak de l'autre.

```text
Granit.Identity              ← IIdentityProvider, modèles, NullIdentityProvider
      ↑
Granit.Identity.Keycloak     ← KeycloakIdentityProvider, token exchange
      (futur)
Granit.Identity.Auth0        ← [DependsOn(Identity)]
Granit.Identity.Entra        ← [DependsOn(Identity)]
```

## Packages

| Package | Rôle | Module |
| --- | --- | --- |
| `Granit.Identity` | `IIdentityProvider` (abstraction), modèles, `NullIdentityProvider` | `GranitIdentityModule` |
| `Granit.Identity.Keycloak` | Keycloak Admin REST API + Account API (token exchange) | `GranitIdentityKeycloakModule` |

---

## Granit.Identity

Package d'abstractions pures. Aucune dépendance externe en dehors de `Granit.Core`.

### Installation

```bash
dotnet add package Granit.Identity
```

### IIdentityProvider

Interface centrale. Un `NullIdentityProvider` est enregistré par défaut (retourne
des listes vides et des no-ops). Installer un package d'implémentation remplace le
null object.

```csharp
public interface IIdentityProvider
{
    // --- Lecture utilisateurs ---
    Task<IReadOnlyList<IdentityUser>> GetUsersAsync(
        string? search = null, int? first = null, int? max = null,
        CancellationToken cancellationToken = default);

    Task<IdentityUser?> GetUserAsync(
        string userId, CancellationToken cancellationToken = default);

    // --- Gestion du compte ---
    Task SetUserEnabledAsync(                    // écriture — exceptions propagées
        string userId, bool enabled,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetPasswordChangedAtAsync(
        string userId, CancellationToken cancellationToken = default);

    // --- Rôles ---
    Task<IReadOnlyList<IdentityRole>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentityUser>> GetRoleMembersAsync(
        string roleName, CancellationToken cancellationToken = default);
}
```

> **Conventions d'erreur :** les opérations de lecture appliquent la *graceful degradation*
> (log warning + retour vide/null si le provider est indisponible). `SetUserEnabledAsync`
> propage les exceptions HTTP pour que l'appelant soit informé d'un échec.

### Modèles

```csharp
// Utilisateur dans l'IDP externe
record IdentityUser(
    string Id, string? Username, string? Email,
    string? FirstName, string? LastName, bool Enabled);

// Rôle dans l'IDP externe
record IdentityRole(string Id, string Name, string? Description);

// Session SSO active
record IdentitySession(
    string SessionId, string? IpAddress,
    DateTimeOffset StartedAt, DateTimeOffset LastAccess,
    bool RememberMe, IReadOnlyList<string> Clients);

// Activité par device (regroupe une ou plusieurs sessions)
// Device / Os / OsVersion / Browser sont null si le provider
// ne supporte pas la vue device (ex. fallback Admin API Keycloak)
record IdentityDeviceActivity(
    string? IpAddress, DateTimeOffset LastAccess,
    string? Device, string? Os, string? OsVersion, string? Browser,
    bool Mobile, bool Current,
    IReadOnlyList<IdentitySession> Sessions);
```

---

## Granit.Identity.Keycloak

Implémentation `IIdentityProvider` via le Keycloak Admin REST API.
Pour la **device activity enrichie** (OS, browser, device), utilise
optionnellement l'Account API via un token exchange OAuth 2.0.

### Installation

```bash
dotnet add package Granit.Identity.Keycloak
```

### Program.cs

Via le système de modules :

```csharp
[DependsOn(typeof(GranitIdentityKeycloakModule))]
public sealed class MyAppModule : GranitModule { ... }
```

Enregistrement direct :

```csharp
builder.Services.AddGranitIdentityKeycloak();
```

### Configuration

```json
{
  "KeycloakAdmin": {
    "BaseUrl": "https://keycloak.example.com",
    "Realm": "my-realm",
    "ClientId": "admin-service",
    "ClientSecret": "*** (Vault)",
    "UseTokenExchangeForDeviceActivity": false
  }
}
```

| Propriété | Type | Requis | Description |
| --------- | ---- | ------ | ----------- |
| `BaseUrl` | `string` | ✅ | URL de base Keycloak (sans le path realm) |
| `Realm` | `string` | ✅ | Nom du realm |
| `ClientId` | `string` | ✅ | Client ID du service account |
| `ClientSecret` | `string` | ✅ | Secret du service account — **charger depuis Vault** |
| `UseTokenExchangeForDeviceActivity` | `bool` | ❌ | Active l'Account API via token exchange pour les infos device (défaut : `false`) |

> `ClientSecret` ne doit **jamais** être stocké en clair.
> Utiliser [Granit.Vault](vault.md) pour injecter le secret dynamiquement.

### Rôles requis sur le service account

| Opération | Rôle Keycloak requis |
| --------- | --------------------- |
| Lecture users, sessions, credentials | `realm-management:view-users` |
| `SetUserEnabledAsync` | `realm-management:manage-users` |
| `GetUserDeviceActivityAsync` avec token exchange | `realm-management:impersonation` + feature `admin-fine-grained-authz` |

### Endpoints Keycloak utilisés

| Méthode `IIdentityProvider` | Endpoint Keycloak Admin REST API |
| --------------------------- | -------------------------------- |
| `GetUsersAsync` | `GET /admin/realms/{realm}/users` |
| `GetUserAsync` | `GET /admin/realms/{realm}/users/{id}` |
| `SetUserEnabledAsync` | `PUT /admin/realms/{realm}/users/{id}` |
| `GetUserSessionsAsync` | `GET /admin/realms/{realm}/users/{id}/sessions` |
| `GetUserDeviceActivityAsync` (sans token exchange) | `GET /admin/realms/{realm}/users/{id}/sessions` — Device/Os/Browser = `null` |
| `GetUserDeviceActivityAsync` (avec token exchange) | `GET /realms/{realm}/account/sessions/devices` (Account API, user token) |
| `GetPasswordChangedAtAsync` | `GET /admin/realms/{realm}/users/{id}/credentials` → credential `type=password`, champ `createdDate` |
| `GetRolesAsync` | `GET /admin/realms/{realm}/roles` |
| `GetRoleMembersAsync` | `GET /admin/realms/{realm}/roles/{name}/users` |

### Device activity — mode token exchange

L'Account API Keycloak (`/account/sessions/devices`) identifie l'utilisateur via
le bearer token. Pour l'appeler depuis un service account admin, Granit utilise le
**OAuth 2.0 Token Exchange** (RFC 8693) :

```text
1. Admin token (service account, déjà en cache)
        │
        ▼
2. POST /realms/{realm}/protocol/openid-connect/token
        grant_type=urn:ietf:params:oauth:grant-type:token-exchange
        requested_subject={userId}
        → user token (court-lived, non caché)
        │
        ▼
3. GET /realms/{realm}/account/sessions/devices
        Authorization: Bearer {user token}
        → DeviceRepresentation[] avec Os, Browser, Device, Mobile, Current
```

**Prérequis Keycloak :**

- Feature `admin-fine-grained-authz` activée sur le realm
- Rôle `realm-management:impersonation` sur le service account

**Activer dans la configuration :**

```json
{
  "KeycloakAdmin": {
    "UseTokenExchangeForDeviceActivity": true
  }
}
```

### Services enregistrés

| Service | Implémentation | Lifetime |
| ------- | -------------- | -------- |
| `IIdentityProvider` | `KeycloakIdentityProvider` | Singleton |
| `KeycloakAdminTokenService` | — | Singleton (token admin mis en cache) |
| `KeycloakUserTokenExchangeService` | — | Transient (pas de cache, token user-specific) |

### Token admin — cache

`KeycloakAdminTokenService` met en cache le token `client_credentials` du service
account avec une marge de sécurité de 30 secondes avant expiration. La classe est
thread-safe (double-check avec `SemaphoreSlim`).

---

## Exemple — administration utilisateurs

```csharp
// Injecter IIdentityProvider depuis le conteneur DI
public sealed class UserAdminService(IIdentityProvider identityProvider)
{
    public async Task<IdentityUser?> GetUserAsync(string userId, CancellationToken ct)
        => await identityProvider.GetUserAsync(userId, ct);

    public async Task DisableUserAsync(string userId, CancellationToken ct)
        // SetUserEnabledAsync propage l'exception si Keycloak répond 4xx/5xx
        => await identityProvider.SetUserEnabledAsync(userId, false, ct);

    public async Task<IReadOnlyList<IdentitySession>> GetActiveSessionsAsync(
        string userId, CancellationToken ct)
        => await identityProvider.GetUserSessionsAsync(userId, ct);

    public async Task<IReadOnlyList<IdentityDeviceActivity>> GetDeviceActivityAsync(
        string userId, CancellationToken ct)
        => await identityProvider.GetUserDeviceActivityAsync(userId, ct);

    public async Task<DateTimeOffset?> GetPasswordChangedAtAsync(
        string userId, CancellationToken ct)
        => await identityProvider.GetPasswordChangedAtAsync(userId, ct);
}
```

---

## Ajouter un nouveau provider (ex. Auth0)

Créer `Granit.Identity.Auth0` en dépendant uniquement de `Granit.Identity` :

```xml
<ProjectReference Include="..\Granit.Identity\..." />
```

```csharp
[DependsOn(typeof(GranitIdentityModule))]
public sealed class GranitIdentityAuth0Module : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
        => context.Services.AddIdentityProvider<Auth0IdentityProvider>();
}
```

Implémenter `IIdentityProvider` dans `Auth0IdentityProvider`.
`Device`, `Os`, `Browser` peuvent être renseignés nativement (Auth0 Management API
expose ces informations dans les sessions).

## Tests

### Granit.Identity.Tests

Les tests de `Granit.Identity` couvrent :

| Classe | Ce qui est testé |
| ------ | ---------------- |
| `NullIdentityProviderTests` | Toutes les méthodes de `NullIdentityProvider` : retours vides/null, absence d'exception pour `SetUserEnabledAsync` |

**Exemple — tester un service applicatif avec `NSubstitute`**

```csharp
// Mocker IIdentityProvider avec NSubstitute
IIdentityProvider identityProvider = Substitute.For<IIdentityProvider>();
identityProvider
    .GetUserAsync("user-1", Arg.Any<CancellationToken>())
    .Returns(new IdentityUser("user-1", "alice", "alice@test.com", "Alice", "Doe", true));

UserAdminService sut = new(identityProvider);
IdentityUser? user = await sut.GetUserAsync("user-1", CancellationToken.None);

user.ShouldNotBeNull();
user.Username.ShouldBe("alice");
```

**`FakeIdentityProvider`** — implémentation minimale fournie dans `Granit.Identity.Tests`
pour les tests d'enregistrement DI :

```csharp
// Retourne des listes vides / null / Task.CompletedTask pour chaque méthode.
internal sealed class FakeIdentityProvider : IIdentityProvider { ... }
```

---

### Granit.Identity.Keycloak.Tests

Les tests de `Granit.Identity.Keycloak` couvrent :

| Classe | Ce qui est testé |
| ------ | ---------------- |
| `KeycloakAdminOptionsTests` | Génération correcte de tous les endpoints (URL, query string, encoding, trailing slash) |
| `KeycloakIdentityProviderTests` | Comportement de `KeycloakIdentityProvider` pour chaque méthode |

#### Infrastructure de test HTTP

Les tests utilisent des handlers de substitution (`HttpMessageHandler`) injectés dans
le `HttpClient` via `IHttpClientFactory` mocké (NSubstitute) :

```csharp
// MockHttpMessageHandler — réponse unique configurable, capture les requêtes
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    public List<(string Method, string Url, string Body)> Requests { get; }
    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;
    public string ResponseBody { get; set; } = string.Empty;
}

// MockSequenceHttpMessageHandler — réponses différentes selon l'ordre d'appel
// Utilisé pour le token exchange : réponse 1 = token, réponse 2 = données Account API
internal sealed class MockSequenceHttpMessageHandler(IReadOnlyList<string> responses)
    : HttpMessageHandler { ... }
```

#### Couverture par méthode

| Méthode | Cas couverts |
| ------- | ------------ |
| `GetUsersAsync` | Résultats avec search/pagination, liste vide, Keycloak indisponible (graceful), mapping des champs |
| `GetUserAsync` | Utilisateur existant, Keycloak 404 (null), mapping complet, `Enabled = false` |
| `GetRolesAsync` | Liste de rôles, liste vide, Keycloak 500 (graceful), `Description = null` |
| `GetRoleMembersAsync` | Utilisateurs renvoyés, rôle vide, Keycloak 503, endpoint correct, encoding du nom de rôle |
| `SetUserEnabledAsync` | PUT avec `enabled: true`, PUT avec `enabled: false`, Keycloak 403 (exception propagée), guard null |
| `GetUserSessionsAsync` | Sessions mappées, liste vide, Keycloak 503 (graceful), endpoint correct, guard null |
| `GetUserDeviceActivityAsync` | Fallback Admin API (Device/Os/Browser = null), Account API avec token exchange (OS/Browser renseignés), Keycloak 503 (graceful), guard null |
| `GetPasswordChangedAtAsync` | Credential `password` trouvé, credential `password` absent, liste vide, Keycloak 503 (graceful), endpoint correct, guard null |

#### Token exchange dans les tests

Quand `UseTokenExchangeForDeviceActivity = true`, deux appels HTTP successifs sont émis :
le POST de token exchange puis le GET Account API. Le `MockSequenceHttpMessageHandler`
permet de retourner une réponse différente à chaque appel :

```csharp
MockSequenceHttpMessageHandler seqHandler = new(
[
    // Appel 1 — POST token exchange → access_token utilisateur
    """{"access_token":"user-token","expires_in":300}""",
    // Appel 2 — GET /account/sessions/devices → liste de devices
    """[{"os":"Windows","browser":"Chrome/120.0","device":"Desktop",...}]""",
]);
```

---

## Dépendances Granit

| Package | Dépend de | Utilisé par |
| ------- | --------- | ----------- |
| `Granit.Identity` | `Granit.Core` | `Granit.Identity.Keycloak` |
| `Granit.Identity.Keycloak` | `Granit.Identity` | Module feuille |

> Voir le [graphe de dépendances complet](../dependencies.md).
