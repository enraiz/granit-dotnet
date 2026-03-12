# Granit.RateLimiting

Per-tenant rate limiting for Granit APIs. Sliding window, fixed window, and token bucket algorithms via Redis Lua scripts. Plan-based quotas via Granit.Features integration. ASP.NET Core endpoint filter (429 + Retry-After) and Wolverine middleware.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.RateLimiting
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/api/rate-limiting.md).
