using Granit.Core.Modularity;
using Granit.DataExchange.EntityFrameworkCore.Internal;
using Granit.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataExchange.EntityFrameworkCore;

/// <summary>
/// Module for the EF Core persistence layer of <c>Granit.DataExchange</c>.
/// Provides <c>DataExchangeDbContext</c>, mapping store, import job store,
/// identity resolvers, and batched import executor.
/// </summary>
/// <remarks>
/// On application initialization, ensures the database schema exists
/// via <see cref="DatabaseFacade.EnsureCreatedAsync"/>. In production,
/// tables are managed by infrastructure (Terraform / SQL scripts).
/// </remarks>
[DependsOn(typeof(GranitDataExchangeModule))]
[DependsOn(typeof(GranitPersistenceModule))]
public sealed class GranitDataExchangeEntityFrameworkCoreModule : GranitModule
{
    /// <inheritdoc />
    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        IDbContextFactory<DataExchangeDbContext> factory = context.ServiceProvider
            .GetRequiredService<IDbContextFactory<DataExchangeDbContext>>();

        await using DataExchangeDbContext dbContext = await factory
            .CreateDbContextAsync()
            .ConfigureAwait(false);

        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}
