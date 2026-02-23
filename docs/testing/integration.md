# Tests d'intégration EF Core

[← Index des tests](index.md)

Les tests d'intégration EF Core utilisent une base de données in-memory pour tester
le pipeline complet (intercepteurs, requêtes) sans infrastructure externe.

## DbContext in-memory

Les tests d'intercepteurs EF Core utilisent `UseInMemoryDatabase` avec un nom unique
par test pour garantir l'isolation :

```csharp
private TestDbContext CreateContext()
{
    var interceptor = new AuditedEntityInterceptor(
        _currentUserService, _clock, _guidGenerator);
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .AddInterceptors(interceptor)
        .Options;
    return new TestDbContext(options);
}
```

## Entités de test internes

Chaque classe de test définit ses propres entités et `DbContext` comme classes
internes `private sealed class`. Cela évite les dépendances entre tests et rend
chaque fichier de test autosuffisant :

```csharp
private sealed class TestEntity : AuditedEntity
{
    public string Name { get; set; } = string.Empty;
}

private sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>()
            .Property(e => e.Id).ValueGeneratedNever();
    }
}
```

> `ValueGeneratedNever()` est nécessaire car l'intercepteur `AuditedEntityInterceptor`
> gère lui-même la génération des GUID via `IGuidGenerator`.
