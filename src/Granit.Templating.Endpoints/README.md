# Granit.Templating.Endpoints

Minimal API admin endpoints for managing Scriban templates: CRUD (draft lifecycle),
publish/unpublish, and revision history. All endpoints require the `Templates.Manage` permission
and are protected by Keycloak-based RBAC via `Granit.Authorization`.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Templating.Endpoints
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/templating/index.md).
