# Granit.Notifications

Multi-channel notification engine for Granit. Provides `INotificationPublisher` for publishing
notifications, Wolverine-based transactional fan-out, `INotificationChannel` for pluggable
delivery channels, and Odoo-style entity tracking via `ITrackedEntity`.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Notifications
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/messaging/notifications.md).
