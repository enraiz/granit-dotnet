using Granit.Core.Modularity;

namespace Granit.DataExchange.EntityFrameworkCore;

/// <summary>
/// Module for the EF Core persistence layer of <c>Granit.DataExchange</c>.
/// Provides <c>DataExchangeDbContext</c>, mapping store, import job store,
/// identity resolvers, and batched import executor.
/// </summary>
[DependsOn(typeof(GranitDataExchangeModule))]
public sealed class GranitDataExchangeEntityFrameworkCoreModule : GranitModule;
