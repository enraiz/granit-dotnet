# Granit.DataImport.EntityFrameworkCore

EF Core persistence layer for Granit.DataImport. Provides an isolated `DataImportDbContext`,
`EfMappingStore`, `EfImportJobStore`, identity resolvers (`BusinessKeyResolver`,
`CompositeKeyResolver`, `ExternalIdResolver`), and a batched `EfImportExecutor`.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.DataImport.EntityFrameworkCore
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/data/data-import.md).
