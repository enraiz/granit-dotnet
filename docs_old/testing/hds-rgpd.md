# Tests de conformité ISO 27001 / RGPD

[← Index des tests](index.md)

Certains tests vérifient directement des exigences réglementaires. Ils constituent
une preuve d'audit : leur suppression ou modification doit être tracée et justifiée.

## Audit trail ISO 27001

Les tests de `AuditedEntityInterceptor` vérifient que les champs d'audit sont
correctement remplis, conformément à l'exigence ISO 27001 de traçabilité sur 3 ans :

```csharp
[Fact]
public async Task SaveChangesAsync_OnAdd_SetsCreatedFields()
{
    // Arrange
    await using TestDbContext context = CreateContext();
    TestEntity entity = new() { Name = "Test" };
    context.TestEntities.Add(entity);

    // Act
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Assert
    entity.CreatedAt.ShouldBe(FixedNow);
    entity.CreatedBy.ShouldBe("user-test-123");
    entity.Id.ShouldBe(FixedGuid);
}
```

## Soft delete RGPD

Les tests de `SoftDeleteInterceptor` vérifient que la suppression physique est
convertie en suppression logique, avec capture de `DeletedAt` et `DeletedBy` :

```csharp
[Fact]
public async Task SaveChangesAsync_OnDelete_ConvertToSoftDelete()
{
    // Arrange
    await using TestDbContext context = CreateContext();
    TestSoftDeletableEntity entity = new() { /* ... */ };
    context.Entities.Add(entity);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Supprimer l'entité
    context.Entities.Remove(entity);

    // Act
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Assert — l'entité est soft-deleted (pas physiquement supprimée)
    entity.IsDeleted.ShouldBeTrue();
    entity.DeletedAt.ShouldBe(FixedNow);
    entity.DeletedBy.ShouldBe("user-test-123");
}
```

## UTC uniquement

Les tests de `Clock` vérifient que l'horloge retourne toujours UTC (conformité ISO 27001) :

```csharp
[Fact]
public void Now_IsAlwaysUtc()
{
    DateTimeOffset now = _clock.Now;
    now.Offset.ShouldBe(TimeSpan.Zero,
        "le Clock doit toujours retourner UTC (conformité ISO 27001)");
}
```
