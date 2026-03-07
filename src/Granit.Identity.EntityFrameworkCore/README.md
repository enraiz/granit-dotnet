# Granit.Identity.EntityFrameworkCore

EF Core persistence for Granit.Identity user cache. Provides `UserCacheEntry` entity, `IUserCacheDbContext`,
`CachedUserLookupService` with cache-aside strategy (including incremental stale refresh), login-time sync
middleware, and Wolverine event handlers for real-time identity provider sync.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Identity.EntityFrameworkCore
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/identity/user-cache.md).
