# Granit.ReferenceData.EntityFrameworkCore

EF Core persistence for `Granit.ReferenceData`. Provides
`EfCoreReferenceDataStore<TEntity, TDbContext>` with built-in memory cache,
`ReferenceDataEntityTypeConfiguration<T>` base class, model builder extensions,
and a `ReferenceDataSeedContributor` bridge for the `IDataSeedContributor`
infrastructure.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.ReferenceData.EntityFrameworkCore
```

## Dependencies

- `Granit.Persistence`
- `Granit.ReferenceData`

## Documentation

See the [full documentation](https://granit-fx.dev).
