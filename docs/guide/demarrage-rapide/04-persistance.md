# Étape 4 — Persistance EF Core

Granit enrichit EF Core avec des intercepteurs automatiques pour l'audit HDS
et le soft delete RGPD.

## Ajouter les packages

```bash
dotnet add package Granit.Persistence
dotnet add package Granit.Security
dotnet add package Granit.Guids
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

## Créer le DbContext

Créer `Data/TaskDbContext.cs` :

```csharp
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Domain;

namespace TaskManagement.Api.Data;

/// <summary>
/// EF Core context for the task management application.
/// </summary>
public sealed class TaskDbContext(DbContextOptions<TaskDbContext> options)
    : DbContext(options)
{
    /// <summary>Task items table.</summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
        });
    }
}
```

## Mettre à jour le module

Modifier `TaskManagementModule.cs` pour ajouter les dépendances et enregistrer
le DbContext :

```csharp
using Granit.Core.Modularity;
using Granit.Guids;
using Granit.Persistence;
using Granit.Persistence.Extensions;
using Granit.Persistence.Interceptors;
using Granit.Security;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Api.Data;

namespace TaskManagement.Api;

[DependsOn(typeof(GranitTimingModule))]
[DependsOn(typeof(GranitGuidsModule))]
[DependsOn(typeof(GranitSecurityModule))]
[DependsOn(typeof(GranitPersistenceModule))]
public sealed class TaskManagementModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddDbContext<TaskDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(context.Configuration.GetConnectionString("Default"));

            // Intercepteurs Granit : audit HDS + soft delete RGPD
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditedEntityInterceptor>(),
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>());
        });
    }
}
```

## Configuration

Ajouter la chaîne de connexion dans `appsettings.Development.json` :

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=task_management;Username=postgres;Password=dev"
  }
}
```

## Ce que font les intercepteurs

### AuditedEntityInterceptor

Avant chaque `SaveChangesAsync()`, l'intercepteur :

1. Détecte les entités en état `Added` → remplit `CreatedAt` (via `IClock.Now`)
   et `CreatedBy` (via `ICurrentUserService.UserId`)
2. Détecte les entités en état `Modified` → remplit `ModifiedAt` et `ModifiedBy`
3. Si `IGuidGenerator` est enregistré et que `Id == Guid.Empty`,
   génère un GUID séquentiel (optimal pour les index clustered PostgreSQL)

### SoftDeleteInterceptor

Convertit les suppressions physiques en suppressions logiques pour les entités
implémentant `ISoftDeletable` :

1. Détecte les entités en état `Deleted` qui implémentent `ISoftDeletable`
2. Change l'état en `Modified` et positionne `IsDeleted = true`,
   `DeletedAt`, `DeletedBy`
3. Un filtre global EF Core masque automatiquement les enregistrements supprimés

## Créer la base de données

```bash
# Ajouter le package d'outils EF Core (si pas déjà installé)
dotnet tool install --global dotnet-ef

# Créer la migration initiale
dotnet ef migrations add InitialCreate

# Appliquer la migration
dotnet ef database update
```

## Prochaine étape

La base de données est prête. Créons les [endpoints CRUD](05-endpoints.md).

## Référence

- [Persistance EF Core](../../framework/data/persistence.md)
- [Configuration](../../framework/core/configuration/sources.md)
