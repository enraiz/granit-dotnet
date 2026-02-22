using DigitalDynamics.Foundation.ApiDocumentation.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace DigitalDynamics.Foundation.ApiDocumentation.Extensions;

/// <summary>
/// Extensions for enabling Foundation OpenAPI endpoints and the Scalar interactive UI.
/// </summary>
public static class ApiDocumentationApplicationBuilderExtensions
{
    /// <summary>
    /// Maps OpenAPI JSON endpoints (<c>/openapi/v{n}.json</c>) and the Scalar interactive UI.
    /// Always enabled in Development; in Production only when
    /// <see cref="ApiDocumentationOptions.EnableInProduction"/> is <c>true</c>.
    /// </summary>
    public static WebApplication UseFoundationApiDocumentation(this WebApplication app)
    {
        ApiDocumentationOptions options =
            app.Services.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;

        bool shouldEnable = app.Environment.IsDevelopment() || options.EnableInProduction;
        if (!shouldEnable)
        {
            return app;
        }

        foreach (int majorVersion in options.MajorVersions)
        {
            app.MapOpenApi($"/openapi/v{majorVersion}.json");
        }

        app.MapScalarApiReference(scalarOptions => scalarOptions.WithTitle(options.Title));

        return app;
    }
}
