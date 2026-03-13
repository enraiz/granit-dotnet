# Granit.Authorization.Endpoints

Minimal API endpoints for RBAC permission management. Exposes current-user
permissions (`GET /me`), permission definitions (`GET /definitions`), and
admin grant/revoke routes (`GET/PUT/DELETE /roles/{roleName}/...`).

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Authorization.Endpoints
```

## Dependencies

- `Granit.Authorization`

## Documentation

See the [full documentation](https://granit-fx.dev).
