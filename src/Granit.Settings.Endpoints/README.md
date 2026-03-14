# Granit.Settings.Endpoints

Minimal API endpoints for Granit settings management: user-scoped preferences (locale, timezone, custom), global and tenant administration. Includes `SettingsCultureMiddleware` for automatic `CultureInfo`/timezone hydration from user settings.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Settings.Endpoints
```

## Dependencies

- `Granit.Authorization`
- `Granit.Settings`
- `Granit.Timing`
- `Granit.Validation`

## Documentation

See the [full documentation](https://granit-fx.dev).
