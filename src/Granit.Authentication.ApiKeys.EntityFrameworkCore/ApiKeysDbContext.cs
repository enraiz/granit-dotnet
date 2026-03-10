using Granit.Authentication.ApiKeys.EntityFrameworkCore.EntityConfigurations;
using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Granit.Authentication.ApiKeys.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for API key persistence.
/// </summary>
internal sealed class ApiKeysDbContext : DbContext
{
    private readonly ICurrentTenant? _currentTenant;
    private readonly IDataFilter? _dataFilter;

    /// <summary>
    /// Initializes a new instance of <see cref="ApiKeysDbContext"/>.
    /// </summary>
    public ApiKeysDbContext(
        DbContextOptions<ApiKeysDbContext> options,
        ICurrentTenant? currentTenant = null,
        IDataFilter? dataFilter = null)
        : base(options)
    {
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

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
        modelBuilder.ApplyGranitConventions(_currentTenant, _dataFilter);
    }
}
