# Granit.Documents.PublicLinks.BackgroundJobs

Recurring background jobs for `Granit.Documents.PublicLinks` (F18).
Ships a daily `PruneExpiredPublicLinksJob` that purges revoked / expired
links past their retention window.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Documents.PublicLinks.BackgroundJobs
```

## What it does

- Registers `PruneExpiredPublicLinksJob` via `[RecurringJob]` — discovered
  automatically by `Granit.BackgroundJobs` on host startup.
- The handler enumerates each tenant context and removes
  `DocumentPublicLink` rows whose `ExpiresAt` is older than the configured
  retention window, or that were revoked beyond that horizon.

Hosts that don't ship background jobs can omit this package — the public
links module remains fully functional; only the periodic cleanup is lost.
