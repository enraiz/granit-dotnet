# Getting Started

Build a working Granit API in under 5 minutes.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A PostgreSQL database (or any EF Core provider)

## 1. Create the project

```bash
dotnet new web -n MyApi
cd MyApi

dotnet add package Granit.Core
dotnet add package Granit.Timing
dotnet add package Granit.Persistence
dotnet add package Granit.ExceptionHandling
dotnet add package Granit.Observability
```

## 2. Define your root module

Create `AppModule.cs`:

```csharp
using Granit.Core.Modularity;
using Granit.ExceptionHandling;
using Granit.Observability;
using Granit.Persistence;
using Granit.Timing;

namespace MyApi;

[DependsOn(
    typeof(GranitTimingModule),
    typeof(GranitPersistenceModule),
    typeof(GranitExceptionHandlingModule),
    typeof(GranitObservabilityModule))]
public sealed class AppModule : GranitModule;
```

Every `[DependsOn]` is resolved automatically — Granit loads modules in
topological order (dependencies first). You never call `AddXxx()` manually
for a Granit module.

## 3. Write Program.cs

Replace the generated `Program.cs`:

```csharp
using Granit.Core.Extensions;
using Granit.Timing;
using MyApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await builder.AddGranitAsync<AppModule>();

WebApplication app = builder.Build();

await app.UseGranitAsync();

app.MapGet("/", (IClock clock) => new
{
    Message = "Hello from Granit!",
    Time = clock.Now
});

await app.RunAsync();
```

`AddGranitAsync<T>()` discovers and configures all modules.
`UseGranitAsync()` initializes them (middleware, health checks, etc.).

## 4. Configure appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

> For production, add `ConnectionStrings`, Vault, and OpenTelemetry
> configuration. See [observability](../framework/diagnostics/observability.md)
> and [persistence](../framework/data/persistence.md).

## 5. Run

```bash
dotnet run
```

Open `http://localhost:5000` (or the port shown in the console).
You should see:

```json
{
  "message": "Hello from Granit!",
  "time": "2026-03-12T14:30:00+00:00"
}
```

The console shows the Granit module loading log:

```text
info: Granit.Core.Modularity.GranitApplication[0]
      Granit module GranitTimingModule [enabled]
info: Granit.Core.Modularity.GranitApplication[0]
      Granit module GranitPersistenceModule [enabled]
info: Granit.Core.Modularity.GranitApplication[0]
      Granit module AppModule [enabled]
info: Granit.Core.Modularity.GranitApplication[0]
      Granit: 3 modules loaded, 3 enabled
```

## What happened

1. `AddGranitAsync<AppModule>()` discovered all `[DependsOn]` modules
   and called `ConfigureServices()` on each, in dependency order
2. `GranitTimingModule` registered `IClock`, `TimeProvider`, and
   `ICurrentTimezoneProvider`
3. `GranitPersistenceModule` registered EF Core audit interceptors
   (`AuditedEntityInterceptor`, `SoftDeleteInterceptor`, etc.)
4. `GranitObservabilityModule` configured Serilog structured logging
   and OpenTelemetry
5. `UseGranitAsync()` called `OnApplicationInitializationAsync()` on
   each module

## Next steps

- Add a **domain model** and **EF Core persistence**:
  [quick start tutorial](demarrage-rapide/03-domaine.md) (French)
- Explore **all 86 packages**: [package catalogue](../../docs/index.md)
- Learn about the **module system**: [modularity](../framework/core/modularity.md)
- Add **JWT authentication**: [security](../framework/security/index.md)
