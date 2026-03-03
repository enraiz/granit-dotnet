using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Granit.DataExchange.EntityFrameworkCore.Tests.Infrastructure;

/// <summary>
/// InMemory factory for <see cref="TestAppDbContext"/>.
/// Each instance uses the same named database for test isolation.
/// </summary>
internal sealed class InMemoryAppContextFactory(string dbName)
    : IDbContextFactory<TestAppDbContext>
{
    public TestAppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    public Task<TestAppDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
        Task.FromResult(CreateDbContext());
}
