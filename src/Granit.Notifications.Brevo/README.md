# Granit.Notifications.Brevo

Unified Brevo (formerly Sendinblue) provider for `Granit.Notifications`.
Implements `IEmailSender`, `ISmsSender`, and `IWhatsAppSender` via Brevo
Transactional API. Single `BrevoNotificationProvider` class registered as
three Keyed Services with key `"Brevo"`.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Notifications.Brevo
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/messaging/notifications.md).
