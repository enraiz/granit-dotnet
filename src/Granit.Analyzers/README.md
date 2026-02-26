# Granit.Analyzers

Roslyn analyzers enforcing Granit conventions: zero-downtime migrations,
security best practices, and Entity Framework Core usage rules.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Rules

### Migrations

| Rule | Severity | Description |
| ---- | -------- | ----------- |
| GRMIGA001 | Error | DropColumn requires a Contract-phase annotation |
| GRMIGA002 | Error | RenameColumn is not zero-downtime safe |
| GRMIGA003 | Warning | AddColumn NOT NULL without a default value risks a table lock |
| GRMIGA004 | Warning | AlterColumn with a type change requires a Contract-phase annotation |

### Security

| Rule | Severity | Description |
| ---- | -------- | ----------- |
| GRSEC001 | Warning | Avoid direct DateTime/DateTimeOffset clock access — use IClock |
| GRSEC002 | Warning | Avoid Guid.NewGuid() — use IGuidGenerator |
| GRSEC003 | Error | Potential hardcoded secret detected |

### Entity Framework

| Rule | Severity | Description |
| ---- | -------- | ----------- |
| GREF001 | Warning | Use SaveChangesAsync() instead of SaveChanges() |

## Installation

```bash
dotnet add package Granit.Analyzers
```

## Documentation

See the [Granit documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/index.md).
