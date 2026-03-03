# Granit.DataExchange.EntityFrameworkCore

EF Core persistence layer for `Granit.DataExchange`. Provides an isolated
`DataExchangeDbContext` with both import and export stores.

**Import**: `EfMappingStore`, `EfImportJobStore`, identity resolvers
(`BusinessKeyResolver`, `CompositeKeyResolver`, `ExternalIdResolver`),
batched `EfImportExecutor`.

**Export**: `EfExportPresetStore`, `EfExportJobStore`.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.DataExchange.EntityFrameworkCore
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/data/data-exchange.md).
