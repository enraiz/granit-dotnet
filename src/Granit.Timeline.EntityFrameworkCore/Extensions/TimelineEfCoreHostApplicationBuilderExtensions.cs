using Granit.Timeline.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Timeline.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for enabling EF Core persistence in Granit.Timeline.
/// </summary>
public static class TimelineEfCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Replaces the default InMemory stores with durable EF Core implementations
    /// backed by a PostgreSQL database hosted in Europe (OVHcloud FR).
    /// </summary>
    /// <remarks>
    /// Must be called after <c>AddGranitTimeline()</c>.
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="EfCoreTimelineStore"/> — replaces <c>InMemoryTimelineStore</c>.</item>
    ///   <item><see cref="EfCoreTimelineQuery"/> — replaces <c>InMemoryTimelineQuery</c>.</item>
    ///   <item><see cref="TimelineDbContext"/> — registered via <c>IDbContextFactory</c> for thread-safe usage.</item>
    /// </list>
    /// <para>
    /// SOVEREIGNTY: The connection string must point to a database hosted in Europe (OVHcloud FR).
    /// Never use AWS RDS, Azure SQL, or Google Cloud SQL for health data.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration (provider + connection string).</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitTimelineEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<TimelineDbContext>(configure);

        builder.Services.Replace(
            ServiceDescriptor.Scoped<ITimelineStore, EfCoreTimelineStore>());
        builder.Services.Replace(
            ServiceDescriptor.Scoped<ITimelineQuery, EfCoreTimelineQuery>());

        return builder;
    }
}
