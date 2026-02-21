# Security

`DigitalDynamics.Foundation.Security` fournit l'authentification JWT Keycloak, la
transformation des claims et les policies d'autorisation pour les applications .NET
Digital Dynamics.

## Installation

```bash
dotnet add package DigitalDynamics.Foundation.Security
```

## Configuration

### appsettings.json

```json
{
  "Keycloak": {
    "Authority": "https://keycloak.meyers.cloud/realms/guava-health",
    "ClientId": "guava-backend",
    "ClientSecret": "***",
    "RequireHttpsMetadata": true
  }
}
```

### Program.cs

Avec le système de modules (recommandé), `FoundationSecurityModule` est chargé
automatiquement via `AddFoundation<T>()` (voir [modularity.md](modularity.md)).

Pour un enregistrement direct :

```csharp
builder.Services.AddFoundationSecurity(builder.Configuration);
```

Cette méthode configure :

1. Authentification JWT Bearer avec Keycloak
2. Transformation des claims (`realm_access.roles` → `ClaimTypes.Role`)
3. Policies d'autorisation (Authenticated, FhirAccess, Admin)
4. `ICurrentUserService` pour accéder à l'utilisateur courant

## KeycloakOptions

```csharp
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public bool RequireHttpsMetadata { get; set; } = true;
    public string? Audience { get; set; }           // Défaut : ClientId
    public string AdminRole { get; set; } = "admin";
}
```

## Transformation des claims Keycloak

Keycloak stocke les rôles dans le claim `realm_access` au format JSON :

```json
{
  "realm_access": {
    "roles": ["admin", "practitioner"]
  }
}
```

`KeycloakClaimsTransformation` extrait automatiquement ces rôles et les ajoute comme
`ClaimTypes.Role` standard .NET. Cela permet l'utilisation de `[Authorize(Roles = "admin")]`.

## CurrentUserService

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
    var userName = currentUser.UserName;   // claim "preferred_username"
    var email = currentUser.Email;         // claim "email"
    var isAdmin = currentUser.IsInRole("admin");
}
```

## Policies d'autorisation

Trois policies sont enregistrées automatiquement :

| Policy | Exigence |
| --- | --- |
| `Authenticated` | Utilisateur authentifié |
| `FhirAccess` | Utilisateur authentifié + scope `fhir-access` |
| `Admin` | Rôle configuré dans `KeycloakOptions.AdminRole` (défaut : `admin`) |

```csharp
// Usage dans un endpoint Wolverine HTTP
[WolverineGet("/api/v1/admin/users")]
[Authorize(Policy = "Admin")]
public static Task<IReadOnlyList<UserResponse>> Handle(...)
```

## Architecture

```text
DigitalDynamics.Foundation.Security
├── ICurrentUserService.cs                 (interface, contrat public)
├── Options/
│   └── KeycloakOptions.cs
├── Authentication/
│   ├── CurrentUserService.cs              (ICurrentUserService via HttpContext)
│   └── KeycloakClaimsTransformation.cs    (realm_access.roles → ClaimTypes.Role)
├── FoundationSecurityModule.cs            (module Foundation)
└── Extensions/
    └── SecurityServiceCollectionExtensions.cs  (AddFoundationSecurity)
```

## Services enregistrés

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `ICurrentUserService` | `CurrentUserService` | Scoped |
| `IClaimsTransformation` | `KeycloakClaimsTransformation` | Transient |
| `IHttpContextAccessor` | Framework | Singleton |

## Validation du token

La configuration JWT Bearer valide :

- **Issuer** : doit correspondre à `Authority`
- **Audience** : doit correspondre à `Audience` (ou `ClientId`)
- **Lifetime** : le token ne doit pas être expiré
- **Signing key** : la signature est vérifiée via les clés JWKS de Keycloak
