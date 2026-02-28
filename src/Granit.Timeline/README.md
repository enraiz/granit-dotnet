# Granit.Timeline

Unified activity stream engine for Granit entities (inspired by Odoo Chatter).
Aggregates comments, internal notes, and system logs per entity via `ITimelined`
marker interface. Provides `ITimelineStore`, `ITimelineQuery`,
`ITimelineFollowerService`, and `ITimelineNotifier` abstractions.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Timeline
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/timeline/index.md).
