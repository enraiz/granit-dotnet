# Granit.BackgroundJobs.Wolverine

Wolverine integration for the Granit background jobs engine. Replaces the default
in-process channel dispatch with durable outbox scheduling via `IMessageBus`,
cluster-safe `SingularAgent` for singleton scheduling, and atomic rescheduling
middleware.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.BackgroundJobs.Wolverine
```

## Dependencies

- `Granit.BackgroundJobs`
- `Granit.Wolverine`

## Documentation

See the [full documentation](https://granit-fx.dev).
