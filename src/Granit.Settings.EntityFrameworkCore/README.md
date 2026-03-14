# Granit.Settings.EntityFrameworkCore

EF Core persistence for Granit.Settings. The consuming application implements ISettingsDbContext on its existing DbContext — zero additional connections. ISO 27001 audit via AuditedEntityInterceptor.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Settings.EntityFrameworkCore
```

## Dependencies

- `Granit.Persistence`
- `Granit.Settings`

## Documentation

See the [full documentation](https://granit-fx.dev).
