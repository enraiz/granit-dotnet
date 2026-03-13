# Granit.Timeline

Unified activity stream engine for Granit entities (inspired by Odoo Chatter).
Aggregates comments, internal notes, and system logs per entity via `ITimelined`
marker interface. Provides `ITimelineStore`, `ITimelineQuery`,
`ITimelineFollowerService`, and `ITimelineNotifier` abstractions.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Timeline
```

## Dependencies

- `Granit.Core`
- `Granit.Guids`
- `Granit.Querying`
- `Granit.Security`
- `Granit.Timing`

## Documentation

See the [full documentation](https://granit-fx.dev).
