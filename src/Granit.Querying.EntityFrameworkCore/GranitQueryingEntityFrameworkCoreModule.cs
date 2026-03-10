using Granit.Core.Modularity;
using Granit.Persistence;

namespace Granit.Querying.EntityFrameworkCore;

/// <summary>
/// Module for the EF Core persistence layer of <c>Granit.Querying</c>.
/// Provides <c>QueryingDbContext</c>, <c>IQueryEngine&lt;T&gt;</c>,
/// and <c>EfCoreSavedViewStore</c>.
/// </summary>
[DependsOn(
    typeof(GranitQueryingModule),
    typeof(GranitPersistenceModule))]
public sealed class GranitQueryingEntityFrameworkCoreModule : GranitModule;
