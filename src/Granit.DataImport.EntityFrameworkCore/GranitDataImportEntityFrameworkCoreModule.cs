using Granit.Core.Modularity;

namespace Granit.DataImport.EntityFrameworkCore;

/// <summary>
/// Module for the EF Core persistence layer of <c>Granit.DataImport</c>.
/// Provides <c>DataImportDbContext</c>, mapping store, import job store,
/// identity resolvers, and batched import executor.
/// </summary>
[DependsOn(typeof(GranitDataImportModule))]
public sealed class GranitDataImportEntityFrameworkCoreModule : GranitModule;
