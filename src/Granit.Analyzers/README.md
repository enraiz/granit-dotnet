# Granit.Analyzers

Roslyn analyzers enforcing Granit conventions: zero-downtime migrations,
security best practices, and Entity Framework Core usage rules.

Part of the [granit](https://granit-fx.dev) framework.

## Rules

### Migrations

| Rule | Severity | CodeFix | Description |
| ---- | -------- | ------- | ----------- |
| GRMIGA001 | Error | — | DropColumn requires a Contract-phase annotation |
| GRMIGA002 | Error | — | RenameColumn is not zero-downtime safe |
| GRMIGA003 | Warning | — | AddColumn NOT NULL without a default value risks a table lock |
| GRMIGA004 | Warning | — | AlterColumn with a type change requires a Contract-phase annotation |

### Security

| Rule | Severity | CodeFix | Description |
| ---- | -------- | ------- | ----------- |
| GRSEC001 | Warning | Yes | Avoid direct DateTime/DateTimeOffset clock access — use IClock |
| GRSEC002 | Warning | Yes | Avoid Guid.NewGuid() — use IGuidGenerator |
| GRSEC003 | Error | — | Potential hardcoded secret detected |

### Entity Framework

| Rule | Severity | CodeFix | Description |
| ---- | -------- | ------- | ----------- |
| GREF001 | Warning | Yes | Use SaveChangesAsync() instead of SaveChanges() |

## Installation

```bash
dotnet add package Granit.Analyzers
```

## Documentation

See the [Granit documentation](https://granit-fx.dev).
