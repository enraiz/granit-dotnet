using Granit.BackgroundJobs.EntityFrameworkCore.Internal;
using Granit.BackgroundJobs.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.BackgroundJobs.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core persistence for Granit background jobs.
/// </summary>
public static class BackgroundJobsEntityFrameworkCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers EF Core persistence for Granit background jobs.
    /// </summary>
    /// <remarks>
    /// Replaces the default <c>InMemoryBackgroundJobStore</c> registered by
    /// <c>AddGranitBackgroundJobs()</c> with <see cref="EfBackgroundJobStore"/>,
    /// and registers <see cref="BackgroundJobsDbContext"/> via
    /// <see cref="EntityFrameworkServiceCollectionExtensions.AddDbContextFactory{TContext}(IServiceCollection, Action{DbContextOptionsBuilder}?, ServiceLifetime)"/>.
    /// <para>
    /// Must be called after <c>AddGranitBackgroundJobs()</c>.
    /// </para>
    /// <para>
    /// The connection string must point to a database hosted in Europe (OVHcloud FR).
    /// Never use a service subject to the US Cloud Act for health data.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration (provider + connection string).</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitBackgroundJobsEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<BackgroundJobsDbContext>(configure);

        builder.Services.Replace(
            ServiceDescriptor.Singleton<IBackgroundJobStore, EfBackgroundJobStore>());

        return builder;
    }
}
