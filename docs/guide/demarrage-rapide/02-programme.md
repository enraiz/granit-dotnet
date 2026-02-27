# Étape 2 — Programme minimal

Créons un projet ASP.NET Core avec Granit et vérifions qu'il démarre.

## Créer le projet

```bash
mkdir -p TaskManagement/src/TaskManagement.Api
cd TaskManagement/src/TaskManagement.Api

dotnet new web
dotnet add package Granit.Core
dotnet add package Granit.Timing
```

## Module racine

Créer `TaskManagementModule.cs` :

```csharp
using Granit.Core.Modularity;
using Granit.Timing;

namespace TaskManagement.Api;

[DependsOn(typeof(GranitTimingModule))]
public sealed class TaskManagementModule : GranitModule
{
}
```

Pour l'instant, le module est vide. Il déclare uniquement sa dépendance vers
`GranitTimingModule` (qui fournit `IClock` et `TimeProvider`).

## Programme

Modifier `Program.cs` :

```csharp
using Granit.Core.Extensions;
using Granit.Timing;
using TaskManagement.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await builder.AddGranitAsync<TaskManagementModule>();

WebApplication app = builder.Build();

await app.UseGranitAsync();

// Endpoint de test : affiche l'heure via IClock
app.MapGet("/", (IClock clock) => new
{
    Message = "TaskManagement API",
    CurrentTime = clock.Now
});

await app.RunAsync();
```

## Vérification

```bash
dotnet run
```

Ouvrir `http://localhost:5000` (ou le port affiché) dans un navigateur.
La réponse JSON contient l'heure UTC :

```json
{
  "message": "TaskManagement API",
  "currentTime": "2026-02-27T10:30:00+00:00"
}
```

## Ce qui s'est passé

1. `AddGranitAsync<TaskManagementModule>()` a découvert `GranitTimingModule`
   (via `[DependsOn]`) et l'a configuré en premier
2. `GranitTimingModule.ConfigureServices()` a enregistré `IClock`, `TimeProvider`
   et `ICurrentTimezoneProvider` dans le conteneur DI
3. `UseGranitAsync()` a appelé `OnApplicationInitializationAsync()` sur chaque module
4. L'endpoint `/` injecte `IClock` depuis le conteneur et retourne l'heure

## Prochaine étape

Ajoutons un [modèle domaine](03-domaine.md) pour représenter nos tâches.
