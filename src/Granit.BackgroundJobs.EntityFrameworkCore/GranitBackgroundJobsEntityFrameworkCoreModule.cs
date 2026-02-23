using Granit.Core.Modularity;

namespace Granit.BackgroundJobs.EntityFrameworkCore;

/// <summary>
/// Granit module for EF Core persistence of background jobs.
/// Registers <c>BackgroundJobsDbContext</c> and <c>EfBackgroundJobStore</c>.
/// </summary>
[DependsOn(typeof(GranitBackgroundJobsModule))]
public sealed class GranitBackgroundJobsEntityFrameworkCoreModule : GranitModule;
