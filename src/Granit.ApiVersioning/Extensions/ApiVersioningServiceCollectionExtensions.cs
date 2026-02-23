using Asp.Versioning;
using Granit.ApiVersioning.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.ApiVersioning.Extensions;

/// <summary>
/// Extensions for registering Granit API versioning services.
/// </summary>
public static class ApiVersioningServiceCollectionExtensions
{
    /// <summary>
    /// Adds URL-based API versioning with query string fallback.
    /// Primary reader: <c>/api/v{version:apiVersion}/resource</c>.
    /// Fallback reader: <c>?api-version=1.0</c> (visible in access logs).
    /// </summary>
    public static IServiceCollection AddGranitApiVersioning(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        GranitApiVersioningOptions foundationOptions = configuration
            .GetSection(GranitApiVersioningOptions.SectionName)
            .Get<GranitApiVersioningOptions>() ?? new GranitApiVersioningOptions();

        services.Configure<GranitApiVersioningOptions>(
            configuration.GetSection(GranitApiVersioningOptions.SectionName));

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(foundationOptions.DefaultMajorVersion);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = foundationOptions.ReportApiVersions;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
