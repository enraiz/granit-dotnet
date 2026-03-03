using System.Reflection;
using Granit.BackgroundJobs.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.Runtime.Handlers;

namespace Granit.BackgroundJobs.Extensions;

/// <summary>
/// Extension methods for registering Granit background jobs services.
/// </summary>
public static class BackgroundJobsHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Granit background jobs infrastructure (provider-agnostic).
    /// </summary>
    /// <remarks>
    /// Reads <see cref="BackgroundJobsOptions"/> from the <c>"BackgroundJobs"</c>
    /// configuration section and registers:
    /// <list type="bullet">
    ///   <item><see cref="IBackgroundJobManager"/> — admin API (scoped).</item>
    ///   <item><see cref="IBackgroundJobStore"/> — InMemory or EF Core, depending on <see cref="JobStoreMode"/>.</item>
    ///   <item><see cref="RecurringJobSchedulingMiddleware"/> — Wolverine middleware for atomic rescheduling, injected via <c>opts.Policies.AddMiddleware</c>.</item>
    /// </list>
    /// <para>
    /// Jobs declared in the calling assembly (and any additional assemblies passed via
    /// <paramref name="additionalAssemblies"/>) are seeded into the store on startup.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="additionalAssemblies">
    /// Additional assemblies to scan for <see cref="RecurringJobAttribute"/>.
    /// The entry assembly is always scanned automatically.
    /// </param>
    /// <param name="configure">Optional additional Wolverine configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitBackgroundJobs(
        this IHostApplicationBuilder builder,
        IEnumerable<Assembly>? additionalAssemblies = null,
        Action<WolverineOptions>? configure = null)
    {
        // Bind and validate options at startup.
        builder.Services
            .AddOptions<BackgroundJobsOptions>()
            .BindConfiguration(BackgroundJobsOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<BackgroundJobsOptions>,
            BackgroundJobsOptionsValidator>();

        // Read options directly from IConfiguration — DI container not yet built.
        BackgroundJobsOptions options = new();
        builder.Configuration
            .GetSection(BackgroundJobsOptions.SectionName)
            .Bind(options);

        // Register InMemory store as the default. When Mode = Durable, the host application
        // must call AddGranitBackgroundJobsEntityFrameworkCore() (Granit.BackgroundJobs.EntityFrameworkCore)
        // which replaces this registration with EfBackgroundJobStore.
        builder.Services.AddSingleton<IBackgroundJobStore, InMemoryBackgroundJobStore>();

        builder.Services.AddScoped<IBackgroundJobManager, BackgroundJobManager>();
        builder.Services.AddSingularAgent<CronSchedulerAgent>();

        // Discover and seed recurring jobs from all relevant assemblies.
        IEnumerable<Assembly> scanAssemblies = new[] { Assembly.GetEntryAssembly()! }
            .Concat(additionalAssemblies ?? [])
            .Distinct();

        IReadOnlyList<RecurringJobRegistration> registrations =
            RecurringJobDiscovery.Discover(scanAssemblies);

        // Register Wolverine middleware on all handler chains whose message type carries
        // [RecurringJobAttribute]. Uses the idiomatic IPolicies.AddMiddleware<T>(filter) API
        // so Wolverine resolves constructor parameters from DI and injects Before/AfterAsync
        // methods at code-generation time (zero overhead at runtime).
        builder.Services.ConfigureWolverine(opts =>
        {
            opts.Policies.AddMiddleware<RecurringJobSchedulingMiddleware>(
                (HandlerChain chain) => chain.MessageType
                    .GetCustomAttribute<RecurringJobAttribute>() is not null);
            configure?.Invoke(opts);
        });

        // Seed jobs after the host is built — store must be resolved from DI.
        builder.Services.AddHostedService(sp =>
            new BackgroundJobsSeedService(
                sp.GetRequiredService<IBackgroundJobStore>(),
                registrations));

        return builder;
    }
}
