# Granit.DataExchange.Excel

Excel support for `Granit.DataExchange`.

**Import**: file parser backed by [Sylvan.Data.Excel](https://github.com/MarkPflug/Sylvan.Data.Excel).
Supports `.xlsx`, `.xls` and `.xlsb` via streaming `ExcelDataReader`.

**Export**: `ClosedXmlExportWriter` implementing `IExportWriter` — bold headers,
auto-fit columns, date formatting.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.DataExchange.Excel
```

## Dependencies

- `Granit.DataExchange`

## Documentation

See the [full documentation](https://granit-fx.dev).
