using Granit.DataImport.EntityFrameworkCore.Internal;
using Granit.DataImport.EntityFrameworkCore.Internal.Pipeline;
using Granit.DataImport.EntityFrameworkCore.Internal.Stores;
using Granit.DataImport.Mapping;
using Granit.DataImport.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Granit.DataImport.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering the DataImport EF Core persistence layer on <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class DataImportEfCoreHostBuilderExtensions
{
    /// <summary>
    /// Registers the DataImport EF Core persistence layer, including the isolated
    /// <c>DataImportDbContext</c>, mapping store, import job store, and orchestrator.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Action to configure the database provider (e.g. <c>opts.UseNpgsql(cs)</c>).</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitDataImportEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<DataImportDbContext>(configure);

        builder.Services.AddScoped<IMappingStore, EfMappingStore>();
        builder.Services.AddScoped<IImportJobStore, EfImportJobStore>();
        builder.Services.AddScoped<IImportOrchestrator, EfImportOrchestrator>();

        return builder;
    }
}
