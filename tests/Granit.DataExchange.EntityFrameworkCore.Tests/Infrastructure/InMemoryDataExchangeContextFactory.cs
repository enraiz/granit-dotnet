using Granit.DataExchange.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataExchange.EntityFrameworkCore.Tests.Infrastructure;

/// <summary>
/// InMemory factory for <see cref="DataExchangeDbContext"/>.
/// Each instance uses the same named database for test isolation.
/// </summary>
internal sealed class InMemoryDataExchangeContextFactory(string dbName)
    : IDbContextFactory<DataExchangeDbContext>
{
    public DataExchangeDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<DataExchangeDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    public Task<DataExchangeDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}
