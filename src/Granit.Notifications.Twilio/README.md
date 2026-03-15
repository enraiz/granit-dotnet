# Granit.Notifications.Twilio

Twilio provider for `Granit.Notifications`.
Implements `ISmsSender` and `IWhatsAppSender` via Twilio Messaging API
using form-encoded payloads. Single `TwilioNotificationProvider` class
registered as two Keyed Services with key `"Twilio"`.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Notifications.Twilio
```

## Dependencies

- `Granit.Notifications.Sms`
- `Granit.Notifications.WhatsApp`

## Documentation

See the [full documentation](https://granit-fx.dev).
