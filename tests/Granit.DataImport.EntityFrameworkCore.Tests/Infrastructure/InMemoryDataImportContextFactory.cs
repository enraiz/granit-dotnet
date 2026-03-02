using Granit.DataImport.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Tests.Infrastructure;

/// <summary>
/// InMemory factory for <see cref="DataImportDbContext"/>.
/// Each instance uses the same named database for test isolation.
/// </summary>
internal sealed class InMemoryDataImportContextFactory(string dbName)
    : IDbContextFactory<DataImportDbContext>
{
    public DataImportDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<DataImportDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    public Task<DataImportDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
        Task.FromResult(CreateDbContext());
}
