# Étape 8 — Tests

Les tests font partie de la **Definition of Done** Granit. Chaque package a
son projet de test. Nous utilisons xUnit v3, Shouldly, NSubstitute et Bogus.

## Créer le projet de test

```bash
mkdir -p tests/TaskManagement.Api.Tests
cd tests/TaskManagement.Api.Tests

dotnet new xunit
dotnet add reference ../../src/TaskManagement.Api/TaskManagement.Api.csproj
dotnet add package Shouldly
dotnet add package NSubstitute
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

## Test unitaire de l'entité

Créer `TaskItemTests.cs` :

```csharp
using Shouldly;
using TaskManagement.Api.Domain;

namespace TaskManagement.Api.Tests;

public sealed class TaskItemTests
{
    [Fact]
    public void NewTaskItem_HasDefaultValues()
    {
        TaskItem task = new();

        task.Id.ShouldBe(Guid.Empty);
        task.Title.ShouldBeEmpty();
        task.Description.ShouldBeNull();
        task.IsCompleted.ShouldBeFalse();
        task.DueDate.ShouldBeNull();
        task.CreatedAt.ShouldBe(default);
        task.CreatedBy.ShouldBeEmpty();
        task.ModifiedAt.ShouldBeNull();
        task.ModifiedBy.ShouldBeNull();
    }

    [Fact]
    public void TaskItem_InheritsAuditFields()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TaskItem task = new()
        {
            Title = "Test task",
            CreatedAt = now,
            CreatedBy = "test-user"
        };

        task.CreatedAt.ShouldBe(now);
        task.CreatedBy.ShouldBe("test-user");
    }
}
```

## Test d'intégration avec EF Core in-memory

Créer `TaskDbContextTests.cs` :

```csharp
using Shouldly;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Domain;

namespace TaskManagement.Api.Tests;

public sealed class TaskDbContextTests : IDisposable
{
    private readonly TaskDbContext _db;

    public TaskDbContextTests()
    {
        DbContextOptions<TaskDbContext> options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new TaskDbContext(options);
    }

    [Fact]
    public async Task CanAddAndRetrieveTask()
    {
        TaskItem task = new()
        {
            Id = Guid.NewGuid(),
            Title = "Test task",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user"
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        TaskItem? retrieved = await _db.Tasks.FindAsync(task.Id);

        retrieved.ShouldNotBeNull();
        retrieved.Title.ShouldBe("Test task");
        retrieved.CreatedBy.ShouldBe("test-user");
    }

    [Fact]
    public async Task TitleHasMaxLength200()
    {
        // Vérifier que le modèle EF Core est correctement configuré
        Microsoft.EntityFrameworkCore.Metadata.IEntityType? entityType =
            _db.Model.FindEntityType(typeof(TaskItem));

        entityType.ShouldNotBeNull();

        Microsoft.EntityFrameworkCore.Metadata.IProperty? titleProperty =
            entityType.FindProperty(nameof(TaskItem.Title));

        titleProperty.ShouldNotBeNull();
        titleProperty.GetMaxLength().ShouldBe(200);
    }

    public void Dispose() => _db.Dispose();
}
```

## Test du module avec NSubstitute

Créer `TaskManagementModuleTests.cs` :

```csharp
using Shouldly;
using Granit.Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManagement.Api;
using TaskManagement.Api.Data;

namespace TaskManagement.Api.Tests;

public sealed class TaskManagementModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersDbContext()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:Default"] =
            "Host=localhost;Database=test;Username=test;Password=test";

        TaskManagementModule module = new();
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);

        ServiceProvider sp = builder.Services.BuildServiceProvider();
        TaskDbContext? db = sp.GetService<TaskDbContext>();

        db.ShouldNotBeNull();
    }
}
```

## Lancer les tests

```bash
dotnet test
```

Tous les tests doivent passer avant de pousser du code.

## Conventions de test Granit

| Convention | Exemple |
| --- | --- |
| Nommage | `MethodName_Scenario_ExpectedResult` ou `MethodName_ExpectedBehavior` |
| Un assert par test | Chaque test vérifie un seul comportement |
| Arrange-Act-Assert | Structure claire en trois phases |
| Shouldly | `result.ShouldBe(expected)` (pas `Assert.Equal`) |
| NSubstitute | `Substitute.For<IService>()` pour les mocks |
| InMemoryDatabase | GUID unique par test pour l'isolation |

## Récapitulatif du tutoriel

En 8 étapes, nous avons construit une API complète avec :

- **Système de modules** : architecture modulaire avec chargement topologique
- **Modèle domaine** : entités avec audit trail HDS automatique
- **Persistance** : EF Core + PostgreSQL + intercepteurs Granit
- **Endpoints** : Minimal API avec le pattern `MapXxxEndpoints()`
- **Sécurité** : JWT Keycloak + `ICurrentUserService`
- **Observabilité** : Serilog + OpenTelemetry + OTLP
- **Tests** : xUnit + Shouldly + NSubstitute

## Pour aller plus loin

| Sujet | Documentation |
| --- | --- |
| Multi-tenancy | [multi-tenancy.md](../../framework/data/multi-tenancy.md) |
| Cache distribué | [caching.md](../../framework/data/caching.md) |
| Messaging Wolverine | [wolverine.md](../../framework/messaging/wolverine.md) |
| Feature flags | [features.md](../../framework/saas/features.md) |
| Cookbook | [Recettes pratiques](../../cookbook/index.md) |

## Référence

- [Conventions de test](../../testing/conventions.md)
- [Mocking](../../testing/mocking.md)
- [Tests d'intégration EF Core](../../testing/integration.md)
