using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Tests.Infrastructure;

/// <summary>
/// Application DbContext for tests, containing <see cref="TestEntity"/>.
/// </summary>
internal sealed class TestAppDbContext(DbContextOptions<TestAppDbContext> options)
    : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;
}
