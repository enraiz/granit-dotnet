using Granit.Core.Modularity;

namespace Granit.Querying.EntityFrameworkCore;

/// <summary>
/// Module for the EF Core persistence layer of <c>Granit.Querying</c>.
/// Provides <c>QueryingDbContext</c>, <c>IQueryEngine&lt;T&gt;</c>,
/// and <c>EfCoreSavedViewStore</c>.
/// </summary>
[DependsOn(typeof(GranitQueryingModule))]
public sealed class GranitQueryingEntityFrameworkCoreModule : GranitModule;
