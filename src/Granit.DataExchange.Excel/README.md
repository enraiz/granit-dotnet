# Granit.DataExchange.Excel

Excel support for `Granit.DataExchange`.

**Import**: file parser backed by [Sylvan.Data.Excel](https://github.com/MarkPflug/Sylvan.Data.Excel).
Supports `.xlsx`, `.xls` and `.xlsb` via streaming `ExcelDataReader`.

**Export**: `ClosedXmlExportWriter` implementing `IExportWriter` — bold headers,
auto-fit columns, date formatting.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.DataExchange.Excel
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/data/data-exchange.md).
