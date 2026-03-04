# Granit.Authorization.Endpoints

Minimal API endpoints for RBAC permission management. Exposes current-user
permissions (`GET /me`), permission definitions (`GET /definitions`), and
admin grant/revoke routes (`GET/PUT/DELETE /roles/{roleName}/...`).

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Authorization.Endpoints
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/security/authorization.md).
