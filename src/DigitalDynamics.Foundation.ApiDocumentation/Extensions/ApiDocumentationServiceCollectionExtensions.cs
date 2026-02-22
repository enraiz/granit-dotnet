using Asp.Versioning;
using DigitalDynamics.Foundation.ApiDocumentation.Options;
using DigitalDynamics.Foundation.ApiDocumentation.Transformers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace DigitalDynamics.Foundation.ApiDocumentation.Extensions;

/// <summary>
/// Extensions for registering Foundation OpenAPI documentation services.
/// </summary>
public static class ApiDocumentationServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAPI document generation by reading the <c>"ApiDocumentation"</c> section from configuration.
    /// Each integer in <c>ApiDocumentation:MajorVersions</c> generates one distinct OpenAPI document.
    /// Call <c>app.UseFoundationApiDocumentation()</c> in <c>Program.cs</c> to expose the Scalar UI.
    /// </summary>
    public static IServiceCollection AddFoundationApiDocumentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(ApiDocumentationOptions.SectionName);

        // The .NET configuration binder appends to existing IList values instead of replacing them.
        // Clearing MajorVersions before Bind prevents duplicates when config mirrors the default value.
        ApiDocumentationOptions options = new();
        options.MajorVersions = [];
        section.Bind(options);
        if (options.MajorVersions.Count == 0)
        {
            options.MajorVersions.Add(1);
        }

        services.Configure<ApiDocumentationOptions>(opts =>
        {
            opts.MajorVersions.Clear();
            section.Bind(opts);
            if (opts.MajorVersions.Count == 0)
            {
                opts.MajorVersions.Add(1);
            }
        });

        return RegisterDocuments(services, options);
    }

    /// <summary>
    /// Adds OpenAPI document generation with programmatic configuration.
    /// Use when the version list or API metadata must be set in code rather than appsettings.
    /// Call <c>app.UseFoundationApiDocumentation()</c> in <c>Program.cs</c> to expose the Scalar UI.
    /// </summary>
    public static IServiceCollection AddFoundationApiDocumentation(
        this IServiceCollection services,
        Action<ApiDocumentationOptions> configure)
    {
        ApiDocumentationOptions options = new();
        configure(options);
        services.Configure(configure);
        return RegisterDocuments(services, options);
    }

    private static IServiceCollection RegisterDocuments(
        IServiceCollection services,
        ApiDocumentationOptions options)
    {
        // Transformers must be registered before AddOpenApi to be resolved via DI.
        services.AddTransient<JwtBearerSecuritySchemeTransformer>();
        services.AddTransient<InternalApiDocumentTransformer>();

        foreach (int majorVersion in options.MajorVersions)
        {
            string documentName = $"v{majorVersion}";
            ApiVersion apiVersion = new(majorVersion);

            services.AddOpenApi(documentName, openApiOptions =>
            {
                openApiOptions.AddDocumentTransformer((doc, ctx, ct) =>
                {
                    doc.Info = new OpenApiInfo
                    {
                        Title = options.Title,
                        Version = apiVersion.ToString(),
                        Description = options.Description,
                        Contact = options.ContactEmail is not null
                            ? new OpenApiContact { Email = options.ContactEmail }
                            : null,
                    };
                    return Task.CompletedTask;
                });

                openApiOptions.AddDocumentTransformer<JwtBearerSecuritySchemeTransformer>();
                openApiOptions.AddDocumentTransformer<InternalApiDocumentTransformer>();
            });
        }

        return services;
    }
}
