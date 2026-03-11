# Granit.Notifications.EntityFrameworkCore

EF Core persistence for Granit.Notifications. Provides PostgreSQL-backed stores for
`IUserNotificationStore`, `INotificationPreferenceStore`, `INotificationSubscriptionStore`,
and `INotificationDeliveryStore` (ISO 27001 audit trail). Includes `EntityTrackingInterceptor`
for Odoo-style auto-tracking.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Notifications.EntityFrameworkCore
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/messaging/notifications.md).
