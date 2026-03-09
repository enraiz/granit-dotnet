using Granit.Authentication.ApiKeys.EntityFrameworkCore.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.Authentication.ApiKeys.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for API key persistence.
/// </summary>
public class ApiKeysDbContext(DbContextOptions<ApiKeysDbContext> options) : DbContext(options)
{
    /// <summary>
    /// API keys table.
    /// </summary>
    public DbSet<ApiKeyEntry> ApiKeys => Set<ApiKeyEntry>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ApiKeyEntryConfiguration());
    }
}
