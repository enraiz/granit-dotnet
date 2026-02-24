# Tests de conformité HDS / RGPD

[← Index des tests](index.md)

Certains tests vérifient directement des exigences réglementaires. Ils constituent
une preuve d'audit : leur suppression ou modification doit être tracée et justifiée.

## Audit trail HDS

Les tests de `AuditedEntityInterceptor` vérifient que les champs d'audit sont
correctement remplis, conformément à l'exigence HDS de traçabilité sur 3 ans :

```csharp
[Fact]
public async Task SaveChangesAsync_OnAdd_SetsCreatedFields()
{
    // Arrange
    await using var context = CreateContext();
    var entity = new TestEntity { Name = "Test" };
    context.TestEntities.Add(entity);

    // Act
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Assert
    entity.CreatedAt.Should().Be(FixedNow);
    entity.CreatedBy.Should().Be("user-test-123");
    entity.Id.Should().Be(FixedGuid);
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
    await using var context = CreateContext();
    var entity = new TestSoftDeletableEntity { /* ... */ };
    context.Entities.Add(entity);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Supprimer l'entité
    context.Entities.Remove(entity);

    // Act
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Assert — l'entité est soft-deleted (pas physiquement supprimée)
    entity.IsDeleted.Should().BeTrue();
    entity.DeletedAt.Should().Be(FixedNow);
    entity.DeletedBy.Should().Be("user-test-123");
}
```

## UTC uniquement

Les tests de `Clock` vérifient que l'horloge retourne toujours UTC (conformité HDS) :

```csharp
[Fact]
public void Now_IsAlwaysUtc()
{
    var now = _clock.Now;
    now.Offset.Should().Be(TimeSpan.Zero,
        "le Clock doit toujours retourner UTC (conformité HDS)");
}
```
