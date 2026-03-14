# Granit.DataExchange.Csv

CSV support for `Granit.DataExchange`.

**Import**: file parser using [Sep](https://github.com/nietras/Sep) (SIMD, zero-alloc, MIT).
Implements `IFileParser` for `text/csv` with streaming `IAsyncEnumerable<RawImportRow>`.

**Export**: `CsvExportWriter` implementing `IExportWriter` — semicolon delimiter,
UTF-8 BOM, RFC 4180 quoting.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.DataExchange.Csv
```

## Dependencies

- `Granit.DataExchange`

## Documentation

See the [full documentation](https://granit-fx.dev).
