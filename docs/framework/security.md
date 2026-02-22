# Security

`DigitalDynamics.Foundation.Security` fournit l'authentification JWT Bearer générique,
`ICurrentUserService` et les policies d'autorisation de base pour les applications .NET
Digital Dynamics.

`DigitalDynamics.Foundation.Security.Keycloak` étend ce module avec les spécificités
Keycloak : transformation des claims, configuration automatique de l'Authority et
policy `Admin`.

Ce découpage suit le pattern ABP Framework : module générique + extensions par IDP.

## Packages

| Package | Rôle |
| --- | --- |
| `Foundation.Security` | JWT Bearer générique, `ICurrentUserService`, policy `Authenticated` |
| `Foundation.Security.Keycloak` | Claims Keycloak, `PostConfigure` JWT Bearer, policy `Admin` |

---

## Foundation.Security

### Installation

```bash
dotnet add package DigitalDynamics.Foundation.Security
```

### Configuration

#### appsettings.json

```json
{
  "Authentication": {
    "Authority": "https://sso.example.com/realms/my-realm",
    "Audience": "my-client",
    "RequireHttpsMetadata": true,
    "NameClaimType": "sub"
  }
}
```

> `NameClaimType` définit quel claim JWT alimente `User.Identity.Name` et donc
> `ICurrentUserService.UserName`. La valeur par défaut `"sub"` est conforme RFC 7519
> (toujours présente dans un token valide).

#### Program.cs

Via le système de modules (recommandé) :

```csharp
// FoundationSecurityModule est chargé automatiquement via AddFoundation<T>()
// Voir docs/framework/modularity.md
```

Enregistrement direct :

```csharp
builder.Services.AddFoundationSecurity(builder.Configuration);
```

### JwtBearerAuthOptions

```csharp
public sealed class JwtBearerAuthOptions
{
    public const string SectionName = "Authentication";

    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string NameClaimType { get; set; } = "sub";  // RFC 7519
}
```

### Services enregistrés

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `ICurrentUserService` | `CurrentUserService` | Scoped |
| `IHttpContextAccessor` | Framework | Singleton |

### Policy enregistrée

| Policy | Exigence |
| --- | --- |
| `Authenticated` | Utilisateur authentifié |

### ICurrentUserService

Implémentation de `ICurrentUserService` basée sur `HttpContext`. Extrait les
informations de l'utilisateur depuis les claims JWT.

```csharp
// Dans un handler Wolverine (method injection)
public static async Task Handle(
    MyCommand command,
    ICurrentUserService currentUser,
    CancellationToken cancellationToken)
{
    var userId = currentUser.UserId;       // claim "sub"
    var userName = currentUser.UserName;   // User.Identity.Name (selon NameClaimType)
    var email = currentUser.Email;         // claim "email"
    var isAdmin = currentUser.IsInRole("admin");
}
```

---

## Foundation.Security.Keycloak

### Installation

```bash
dotnet add package DigitalDynamics.Foundation.Security.Keycloak
```

Ce package dépend de `Foundation.Security` : les deux modules sont enregistrés.

### Configuration

#### appsettings.json

```json
{
  "Authentication": {
    "Authority": "https://keycloak.example.com/realms/my-realm",
    "Audience": "my-client"
  },
  "Keycloak": {
    "Authority": "https://keycloak.example.com/realms/my-realm",
    "ClientId": "my-client",
    "ClientSecret": "***",
    "RequireHttpsMetadata": true,
    "AdminRole": "admin",
    "RoleClaimsSource": "realm_access"
  }
}
```

> La section `"Authentication"` est lue par `Foundation.Security` (module de base).
> La section `"Keycloak"` est lue par `Foundation.Security.Keycloak` qui reconfigure
> ensuite le JWT Bearer via `PostConfigureAll<JwtBearerOptions>`.

#### Program.cs

Via le système de modules (recommandé) :

```csharp
// FoundationSecurityKeycloakModule ([DependsOn(FoundationSecurityModule)])
// est chargé automatiquement via AddFoundation<T>()
```

Enregistrement direct :

```csharp
builder.Services.AddFoundationSecurity(builder.Configuration);
builder.Services.AddFoundationSecurityKeycloak(builder.Configuration);
```

### KeycloakOptions

```csharp
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string? Audience { get; set; }              // Défaut : ClientId
    public string AdminRole { get; set; } = "admin";
    public string RoleClaimsSource { get; set; } = "realm_access";
}
```

#### RoleClaimsSource

| Valeur | Structure JWT Keycloak |
| --- | --- |
| `"realm_access"` (défaut) | `realm_access.roles[]` — rôles du realm |
| `"resource_access"` | `resource_access.{ClientId}.roles[]` — rôles du client |

### Transformation des claims Keycloak

Keycloak stocke les rôles dans le claim `realm_access` (ou `resource_access`) :

```json
{
  "realm_access": { "roles": ["admin", "practitioner"] },
  "resource_access": {
    "my-client": { "roles": ["billing-manager"] }
  }
}
```

`KeycloakClaimsTransformation` extrait ces rôles et les ajoute comme `ClaimTypes.Role`
standard .NET. Cela permet l'utilisation de `[Authorize(Roles = "admin")]` et
`User.IsInRole("admin")`.

### Services supplémentaires enregistrés

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IClaimsTransformation` | `KeycloakClaimsTransformation` | Transient |

### Policies supplémentaires enregistrées

| Policy | Exigence |
| --- | --- |
| `Admin` | Rôle configuré dans `KeycloakOptions.AdminRole` (défaut : `admin`) |

> Les policies métier (ex. `FhirAccess`, `PractitionerOnly`) sont à définir dans
> l'application elle-même, pas dans Foundation.

---

## Architecture

```text
Foundation.Security                          Foundation.Security.Keycloak
├── ICurrentUserService.cs                   ├── FoundationSecurityKeycloakModule.cs
├── Options/                                 │   [DependsOn(FoundationSecurityModule)]
│   └── JwtBearerAuthOptions.cs             ├── Options/
├── Authentication/                          │   └── KeycloakOptions.cs
│   └── CurrentUserService.cs               ├── Authentication/
├── FoundationSecurityModule.cs             │   └── KeycloakClaimsTransformation.cs
└── Extensions/                             └── Extensions/
    └── SecurityServiceCollectionExtensions.cs  └── SecurityKeycloakServiceCollectionExtensions.cs
```

## Validation du token

La configuration JWT Bearer valide :

- **Issuer** : doit correspondre à `Authority`
- **Audience** : doit correspondre à `Audience` (ou `ClientId` pour Keycloak)
- **Lifetime** : le token ne doit pas être expiré
- **Signing key** : la signature est vérifiée via les clés JWKS de l'IDP
