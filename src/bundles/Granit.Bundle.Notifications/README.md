# Granit.Bundle.Notifications

Meta-package grouping Granit notification modules for multi-channel delivery
(InApp, Email, SignalR real-time).

Part of the [Granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Included packages

| Package | Role |
| --- | --- |
| `Granit.Notifications` | Notification engine, fan-out, delivery tracking |
| `Granit.Notifications.EntityFrameworkCore` | EF Core persistence |
| `Granit.Notifications.Endpoints` | Minimal API endpoints |
| `Granit.Notifications.Email` | Email channel abstractions |
| `Granit.Notifications.Email.Smtp` | SMTP email provider |
| `Granit.Notifications.SignalR` | Real-time SignalR channel |

## Installation

```bash
dotnet add package Granit.Bundle.Notifications
```

## Documentation

See the [notifications documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/messaging/notifications.md).
