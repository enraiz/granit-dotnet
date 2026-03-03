# Granit.DataImport.Csv

CSV file parser for `Granit.DataImport` using [Sep](https://github.com/nietras/Sep)
(SIMD, zero-alloc, MIT). Implements `IFileParser` for `text/csv` and `application/csv`
MIME types with streaming via `IAsyncEnumerable<RawImportRow>`.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.DataImport.Csv
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/data/data-import.md).
