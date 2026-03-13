# Granit.Notifications.Brevo

Unified Brevo (formerly Sendinblue) provider for `Granit.Notifications`.
Implements `IEmailSender`, `ISmsSender`, and `IWhatsAppSender` via Brevo
Transactional API. Single `BrevoNotificationProvider` class registered as
three Keyed Services with key `"Brevo"`.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Notifications.Brevo
```

## Dependencies

- `Granit.Notifications.Email`
- `Granit.Notifications.Sms`
- `Granit.Notifications.WhatsApp`

## Documentation

See the [full documentation](https://granit-fx.dev).
