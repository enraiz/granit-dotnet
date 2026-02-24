using Microsoft.EntityFrameworkCore;

namespace Granit.Persistence.MultiTenancy;

internal sealed class SharedDatabaseDbContextOptions<TContext>
    where TContext : DbContext
{
    public required Action<DbContextOptionsBuilder<TContext>> Configure { get; init; }
}
