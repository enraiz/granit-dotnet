# Granit.BackgroundJobs.Wolverine

Wolverine integration for the Granit background jobs engine. Replaces the default
in-process channel dispatch with durable outbox scheduling via `IMessageBus`,
cluster-safe `SingularAgent` for singleton scheduling, and atomic rescheduling
middleware.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.BackgroundJobs.Wolverine
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/scheduling/background-jobs.md).
