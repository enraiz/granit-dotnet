# Granit.DataExchange

Core data exchange infrastructure for the Granit framework.

**Import**: mini-ETL pipeline Extract → Map → Validate → Execute, with a 4-tier
smart mapping suggestion engine (Saved → Exact → Fuzzy → Semantic AI).

**Export**: tabular export (Excel/CSV) with fluent `ExportDefinition<T>`, presets,
background jobs, and roundtrip support (export → modify → reimport).

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.DataExchange
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/data/data-exchange.md).
