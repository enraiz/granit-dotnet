using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.EntityFrameworkCore.Tests;

public sealed class EfCoreApiKeyStoreTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApiKeysDbContext> _options = null!;
    private EfCoreApiKeyStore _sut = null!;

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync(TestContext.Current.CancellationToken);

        _options = new DbContextOptionsBuilder<ApiKeysDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var db = new ApiKeysDbContext(_options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var factory = new TestDbContextFactory(_options);
        _sut = new EfCoreApiKeyStore(factory, NullLogger<EfCoreApiKeyStore>.Instance);
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task FindByHashAsync_ExistingKey_ReturnsEntry()
    {
        await SeedAsync(CreateEntry("hash123"));

        ApiKeyEntry? result = await _sut.FindByHashAsync("hash123", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Key");
    }

    [Fact]
    public async Task FindByHashAsync_NonExistentHash_ReturnsNull()
    {
        ApiKeyEntry? result = await _sut.FindByHashAsync("nonexistent", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindByHashAsync_SoftDeletedKey_ReturnsNull()
    {
        ApiKeyEntry entry = CreateEntry("hash_deleted");
        entry.IsDeleted = true;
        entry.DeletedAt = DateTimeOffset.UtcNow;
        await SeedAsync(entry);

        ApiKeyEntry? result = await _sut.FindByHashAsync("hash_deleted", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateLastUsedAsync_UpdatesTimestamp()
    {
        ApiKeyEntry entry = CreateEntry("hash_used");
        await SeedAsync(entry);

        var usedAt = new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);
        await _sut.UpdateLastUsedAsync(entry.Id, usedAt, TestContext.Current.CancellationToken);

        // Re-fetch with a fresh context to verify
        await using ApiKeysDbContext db = new(_options);
        ApiKeyEntry? updated = await db.ApiKeys
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.Id == entry.Id, TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.LastUsedAt.ShouldBe(usedAt);
    }

    private async Task SeedAsync(ApiKeyEntry entry)
    {
        await using var db = new ApiKeysDbContext(_options);
        db.ApiKeys.Add(entry);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static ApiKeyEntry CreateEntry(string hash) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Key",
        Type = ApiKeyType.Secret,
        Environment = "test",
        HashedKey = hash,
        Prefix = "gk_test_sk_",
        LastFourChars = "abcd",
        Permissions = ["Read"],
        AllowedCidrs = [],
        CreatedBy = "test",
    };

    /// <summary>
    /// Factory that creates new DbContext instances sharing the same SQLite connection.
    /// </summary>
    private sealed class TestDbContextFactory(DbContextOptions<ApiKeysDbContext> options)
        : IDbContextFactory<ApiKeysDbContext>
    {
        public ApiKeysDbContext CreateDbContext() => new(options);
    }
}
