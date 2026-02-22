using Asp.Versioning;
using DigitalDynamics.Foundation.ApiVersioning.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.ApiVersioning.Extensions;

/// <summary>
/// Extensions for registering Foundation API versioning services.
/// </summary>
public static class ApiVersioningServiceCollectionExtensions
{
    /// <summary>
    /// Adds URL-based API versioning with query string fallback.
    /// Primary reader: <c>/api/v{version:apiVersion}/resource</c>.
    /// Fallback reader: <c>?api-version=1.0</c> (visible in access logs).
    /// </summary>
    public static IServiceCollection AddFoundationApiVersioning(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        FoundationApiVersioningOptions foundationOptions = configuration
            .GetSection(FoundationApiVersioningOptions.SectionName)
            .Get<FoundationApiVersioningOptions>() ?? new FoundationApiVersioningOptions();

        services.Configure<FoundationApiVersioningOptions>(
            configuration.GetSection(FoundationApiVersioningOptions.SectionName));

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
