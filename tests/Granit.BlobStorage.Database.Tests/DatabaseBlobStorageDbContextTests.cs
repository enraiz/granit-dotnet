using Granit.BlobStorage.Database.Entities;
using Granit.BlobStorage.Database.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Database.Tests;

public sealed class DatabaseBlobStorageDbContextTests
{
    private static DatabaseBlobStorageDbContext CreateInMemory() =>
        new(new DbContextOptionsBuilder<DatabaseBlobStorageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ── Schema creation ─────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureCreatedAsync_WithInMemoryProvider_DoesNotThrow()
    {
        await using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        Func<Task> act = () => ctx.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    // ── Entity configuration — table mapping ────────────────────────────────

    [Fact]
    public void Model_TableName_IsStorageBlobContents()
    {
        using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        string? tableName = ctx.Model
            .FindEntityType(typeof(DatabaseBlobContent))!
            .GetTableName();

        tableName.ShouldBe("storage_blob_contents");
    }

    // ── Entity configuration — property constraints ─────────────────────────

    [Fact]
    public void Model_ObjectKey_HasMaxLength1024_AndIsRequired()
    {
        using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(DatabaseBlobContent))!
            .FindProperty(nameof(DatabaseBlobContent.ObjectKey));

        property.ShouldNotBeNull();
        property!.GetMaxLength().ShouldBe(1024);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_Content_IsRequired()
    {
        using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(DatabaseBlobContent))!
            .FindProperty(nameof(DatabaseBlobContent.Content));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_CreatedAt_IsRequired()
    {
        using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(DatabaseBlobContent))!
            .FindProperty(nameof(DatabaseBlobContent.CreatedAt));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_TenantId_IsNullable()
    {
        using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(DatabaseBlobContent))!
            .FindProperty(nameof(DatabaseBlobContent.TenantId));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
    }

    // ── Entity configuration — indexes ──────────────────────────────────────

    [Fact]
    public void Model_UniqueIndex_OnObjectKey()
    {
        using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        IEntityType entityType = ctx.Model.FindEntityType(typeof(DatabaseBlobContent))!;

        IIndex? uniqueIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.IsUnique &&
                i.Properties.Any(p => p.Name == nameof(DatabaseBlobContent.ObjectKey)));

        uniqueIndex.ShouldNotBeNull("a unique index on ObjectKey must be configured");
    }

    [Fact]
    public void Model_CompositeIndex_OnTenantIdAndObjectKey()
    {
        using DatabaseBlobStorageDbContext ctx = CreateInMemory();

        IEntityType entityType = ctx.Model.FindEntityType(typeof(DatabaseBlobContent))!;

        IIndex? compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == nameof(DatabaseBlobContent.TenantId)) &&
                i.Properties.Any(p => p.Name == nameof(DatabaseBlobContent.ObjectKey)));

        compositeIndex.ShouldNotBeNull("a composite index on (TenantId, ObjectKey) must be configured");
    }

    // ── CRUD round-trip ─────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndReload_AllFields_MatchOriginal()
    {
        await using DatabaseBlobStorageDbContext ctx = CreateInMemory();
        var tenantId = Guid.NewGuid();
        byte[] content = [0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]; // "%PDF-1.4"

        DatabaseBlobContent entity = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ObjectKey = $"{tenantId}/medical-images/2026/03/blob-id",
            Content = content,
            CreatedAt = new DateTimeOffset(2026, 3, 14, 10, 0, 0, TimeSpan.Zero),
        };

        ctx.BlobContents.Add(entity);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        ctx.ChangeTracker.Clear();

        DatabaseBlobContent? loaded = await ctx.BlobContents
            .FindAsync([entity.Id], TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded!.TenantId.ShouldBe(tenantId);
        loaded.ObjectKey.ShouldBe(entity.ObjectKey);
        loaded.Content.ShouldBe(content);
        loaded.CreatedAt.ShouldBe(entity.CreatedAt);
    }

    [Fact]
    public async Task SaveAndReload_WithNullTenantId_ShouldPersist()
    {
        await using DatabaseBlobStorageDbContext ctx = CreateInMemory();
        byte[] content = [0x89, 0x50, 0x4E, 0x47]; // PNG magic bytes

        DatabaseBlobContent entity = new()
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            ObjectKey = "avatars/2026/03/blob-id",
            Content = content,
            CreatedAt = new DateTimeOffset(2026, 3, 14, 10, 0, 0, TimeSpan.Zero),
        };

        ctx.BlobContents.Add(entity);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        ctx.ChangeTracker.Clear();

        DatabaseBlobContent? loaded = await ctx.BlobContents
            .FindAsync([entity.Id], TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded!.TenantId.ShouldBeNull();
    }
}
