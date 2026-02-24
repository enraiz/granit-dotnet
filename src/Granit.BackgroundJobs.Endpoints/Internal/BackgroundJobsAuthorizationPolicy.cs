namespace Granit.BackgroundJobs.Endpoints.Internal;

/// <summary>
/// Authorization policy constants for background jobs endpoints.
/// </summary>
public static class BackgroundJobsAuthorizationPolicy
{
    /// <summary>
    /// Name of the ASP.NET Core authorization policy that guards all background jobs
    /// administration endpoints.
    /// </summary>
    /// <remarks>
    /// The policy is registered by
    /// <see cref="Extensions.BackgroundJobsEndpointRouteBuilderExtensions.MapBackgroundJobsEndpoints"/>
    /// and requires the role configured via
    /// <see cref="BackgroundJobsEndpointsOptions.RequiredRole"/>.
    /// </remarks>
    public const string PolicyName = "BackgroundJobs.Admin";
}
