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
    Task<IdentityUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);

    // --- Gestion du compte ---
    Task SetUserEnabledAsync(string userId, bool enabled, CancellationToken cancellationToken = default);
    Task<IdentityUser> CreateUserAsync(IdentityUserCreate user, CancellationToken cancellationToken = default);

    // --- Sessions ---
    Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(string userId, CancellationToken cancellationToken = default);
    Task TerminateSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task TerminateAllSessionsAsync(string userId, CancellationToken cancellationToken = default);

    // --- Credentials ---
    Task<DateTimeOffset?> GetPasswordChangedAtAsync(string userId, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string userId, CancellationToken cancellationToken = default);
    Task SetTemporaryPasswordAsync(string userId, string temporaryPassword, CancellationToken cancellationToken = default);
    Task<bool> VerifyUserCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

    // --- Rôles ---
    Task<IReadOnlyList<IdentityRole>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityUser>> GetRoleMembersAsync(string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);

    // --- Groupes ---
    Task<IReadOnlyList<IdentityGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityGroup>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);
    Task AddUserToGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);
    Task RemoveUserFromGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);
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
    string? FirstName, string? LastName, bool Enabled,
    IReadOnlyDictionary<string, string>? Attributes = null);

// Données pour créer un utilisateur
record IdentityUserCreate(
    string Username, string Email,
    string? FirstName = null, string? LastName = null,
    bool Enabled = true, string? TemporaryPassword = null);

// Rôle dans l'IDP externe
record IdentityRole(string Id, string Name, string? Description);

// Groupe dans l'IDP externe (hiérarchie récursive)
record IdentityGroup(
    string Id, string Name, string? Path,
    IReadOnlyList<IdentityGroup> SubGroups);

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
| `DirectAccessClientId` | `string?` | ❌ | Client ID public avec *Direct Access Grants* activé, utilisé par `VerifyUserCredentialsAsync` (ROPC grant) |

> `ClientSecret` ne doit **jamais** être stocké en clair.
> Utiliser [Granit.Vault](vault.md) pour injecter le secret dynamiquement.

### Rôles requis sur le service account

| Opération | Rôle Keycloak requis |
| --------- | --------------------- |
| Lecture users, sessions, credentials, rôles, groupes | `realm-management:view-users` |
| Toutes les opérations d'écriture (enable/disable, create, roles, sessions, password, groups) | `realm-management:manage-users` |
| `GetUserDeviceActivityAsync` avec token exchange | `realm-management:impersonation` + feature `admin-fine-grained-authz` |

### Endpoints Keycloak utilisés

| Méthode `IIdentityProvider` | Endpoint Keycloak Admin REST API |
| --------------------------- | -------------------------------- |
| `GetUsersAsync` | `GET /admin/realms/{realm}/users` |
| `GetUserAsync` | `GET /admin/realms/{realm}/users/{id}` |
| `SetUserEnabledAsync` | `PUT /admin/realms/{realm}/users/{id}` |
| `CreateUserAsync` | `POST /admin/realms/{realm}/users` (+ `PUT .../reset-password` si mot de passe temporaire) |
| `GetUserSessionsAsync` | `GET /admin/realms/{realm}/users/{id}/sessions` |
| `GetUserDeviceActivityAsync` (sans token exchange) | `GET /admin/realms/{realm}/users/{id}/sessions` — Device/Os/Browser = `null` |
| `GetUserDeviceActivityAsync` (avec token exchange) | `GET /realms/{realm}/account/sessions/devices` (Account API, user token) |
| `TerminateSessionAsync` | `DELETE /admin/realms/{realm}/sessions/{sessionId}` |
| `TerminateAllSessionsAsync` | `POST /admin/realms/{realm}/users/{id}/logout` |
| `GetPasswordChangedAtAsync` | `GET /admin/realms/{realm}/users/{id}/credentials` → credential `type=password`, champ `createdDate` |
| `SendPasswordResetEmailAsync` | `PUT /admin/realms/{realm}/users/{id}/execute-actions-email` body: `["UPDATE_PASSWORD"]` |
| `SetTemporaryPasswordAsync` | `PUT /admin/realms/{realm}/users/{id}/reset-password` body: `{ type, value, temporary }` |
| `GetRolesAsync` | `GET /admin/realms/{realm}/roles` |
| `GetRoleMembersAsync` | `GET /admin/realms/{realm}/roles/{name}/users` |
| `GetUserRolesAsync` | `GET /admin/realms/{realm}/users/{id}/role-mappings/realm` |
| `AssignRoleAsync` | `GET .../roles/{name}` puis `POST .../users/{id}/role-mappings/realm` |
| `RemoveRoleAsync` | `GET .../roles/{name}` puis `DELETE .../users/{id}/role-mappings/realm` |
| `GetGroupsAsync` | `GET /admin/realms/{realm}/groups` |
| `GetUserGroupsAsync` | `GET /admin/realms/{realm}/users/{id}/groups` |
| `AddUserToGroupAsync` | `PUT /admin/realms/{realm}/users/{id}/groups/{groupId}` |
| `RemoveUserFromGroupAsync` | `DELETE /admin/realms/{realm}/users/{id}/groups/{groupId}` |
| `VerifyUserCredentialsAsync` | `POST /realms/{realm}/protocol/openid-connect/token` (Resource Owner Password Grant via `DirectAccessClientId`) |

### Vérification de credentials (ROPC)

`VerifyUserCredentialsAsync` permet de ré-authentifier un utilisateur avant une
opération sensible (ex. définir un mot de passe temporaire). La vérification utilise
le **Resource Owner Password Credentials** (ROPC) grant :

```text
POST /realms/{realm}/protocol/openid-connect/token
    grant_type=password
    client_id={DirectAccessClientId}
    username={username}
    password={password}
```

**Prérequis Keycloak :**

- Un client **public** avec *Direct Access Grants Enabled* (ex. `my-frontend`)
- Configurer `DirectAccessClientId` dans `KeycloakAdmin`

```json
{
  "KeycloakAdmin": {
    "DirectAccessClientId": "my-frontend"
  }
}
```

Si `DirectAccessClientId` n'est pas configuré, l'appel lève une
`InvalidOperationException`.

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
    public async Task<IdentityUser?> GetUserAsync(string userId, CancellationToken cancellationToken)
        => await identityProvider.GetUserAsync(userId, ct);

    public async Task DisableUserAsync(string userId, CancellationToken cancellationToken)
        => await identityProvider.SetUserEnabledAsync(userId, false, ct);

    public async Task<IdentityUser> CreateUserAsync(
        string username, string email, CancellationToken cancellationToken)
        => await identityProvider.CreateUserAsync(
            new IdentityUserCreate(username, email, TemporaryPassword: "ChangeMeNow!"), ct);

    public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken)
        => await identityProvider.AssignRoleAsync(userId, role, ct);

    public async Task TerminateAllSessionsAsync(string userId, CancellationToken cancellationToken)
        => await identityProvider.TerminateAllSessionsAsync(userId, ct);

    public async Task SendPasswordResetAsync(string userId, CancellationToken cancellationToken)
        => await identityProvider.SendPasswordResetEmailAsync(userId, ct);
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
| `NullIdentityProviderTests` | Toutes les méthodes de `NullIdentityProvider` : retours vides/null, absence d'exception pour les opérations d'écriture |
| `IdentityUserTests` | Mapping des attributs, valeur par défaut `null` pour `Attributes` |

**`FakeIdentityProvider`** — implémentation minimale fournie dans `Granit.Identity.Tests`
pour les tests d'enregistrement DI (retourne des listes vides / null / `Task.CompletedTask`
pour chaque méthode).

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

// MockHttpMessageHandlerWithLocation — comme MockHttpMessageHandler mais avec
// un header Location configurable (utilisé pour CreateUserAsync, réponse 201)
internal sealed class MockHttpMessageHandlerWithLocation : HttpMessageHandler { ... }

// MockSequenceHttpMessageHandler — réponses différentes selon l'ordre d'appel
// Utilisé pour le token exchange : réponse 1 = token, réponse 2 = données Account API
internal sealed class MockSequenceHttpMessageHandler(IReadOnlyList<string> responses)
    : HttpMessageHandler { ... }
```

#### Couverture par méthode

| Méthode | Cas couverts |
| ------- | ------------ |
| `GetUsersAsync` | Résultats avec search/pagination, liste vide, Keycloak indisponible (graceful), mapping (dont Attributes) |
| `GetUserAsync` | Utilisateur existant, Keycloak 404 (null), mapping complet, `Enabled = false` |
| `GetRolesAsync` | Liste de rôles, liste vide, Keycloak 500 (graceful), `Description = null` |
| `GetRoleMembersAsync` | Utilisateurs renvoyés, rôle vide, Keycloak 503, endpoint correct, encoding du nom de rôle |
| `SetUserEnabledAsync` | PUT avec `enabled: true/false`, Keycloak 403 (exception propagée), guard null |
| `CreateUserAsync` | Création avec mot de passe temporaire, extraction ID depuis header Location |
| `GetUserSessionsAsync` | Sessions mappées, liste vide, Keycloak 503 (graceful), guard null |
| `TerminateSessionAsync` | DELETE session, Keycloak 404, Keycloak 403 (exception), guard null |
| `TerminateAllSessionsAsync` | POST logout, guard null |
| `GetUserDeviceActivityAsync` | Fallback Admin API, Account API avec token exchange, Keycloak 503 (graceful) |
| `GetPasswordChangedAtAsync` | Credential trouvé/absent, liste vide, Keycloak 503 (graceful) |
| `SendPasswordResetEmailAsync` | PUT execute-actions-email, guard null |
| `SetTemporaryPasswordAsync` | PUT reset-password, body correct, guard null |
| `GetUserRolesAsync` | Rôles de l'utilisateur, Keycloak 503 (graceful), guard null, mapping |
| `AssignRoleAsync` | GET role + POST mapping, guard null, Keycloak 403 |
| `RemoveRoleAsync` | GET role + DELETE mapping avec body |
| `GetGroupsAsync` | Groupes avec sous-groupes, liste vide, Keycloak 503 (graceful) |
| `GetUserGroupsAsync` | Groupes de l'utilisateur, liste vide, Keycloak 503 (graceful) |
| `AddUserToGroupAsync` | PUT membership, guard null |
| `RemoveUserFromGroupAsync` | DELETE membership, guard null |
| `VerifyUserCredentialsAsync` | Credentials valides (true), invalides (false), `DirectAccessClientId` absent (exception), guards null |

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
