using Asp.Versioning;
using Granit.ApiDocumentation.Options;
using Granit.ApiDocumentation.Transformers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace Granit.ApiDocumentation.Extensions;

/// <summary>
/// Extensions for registering Granit OpenAPI documentation services.
/// </summary>
public static class ApiDocumentationServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAPI document generation by reading the <c>"ApiDocumentation"</c> section from configuration.
    /// Each integer in <c>ApiDocumentation:MajorVersions</c> generates one distinct OpenAPI document.
    /// Call <c>app.UseGranitApiDocumentation()</c> in <c>Program.cs</c> to expose the Scalar UI.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="IHostApplicationBuilder"/> because OpenAPI document endpoints must be
    /// registered at startup (one per major version), which requires reading configuration before
    /// the DI container is built.
    /// </remarks>
    public static IHostApplicationBuilder AddGranitApiDocumentation(
        this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<ApiDocumentationOptions>()
            .BindConfiguration(ApiDocumentationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        IConfigurationSection section = builder.Configuration.GetSection(ApiDocumentationOptions.SectionName);

        // The .NET configuration binder appends to existing IList values instead of replacing them.
        // Clearing MajorVersions before Bind prevents duplicates when config mirrors the default value.
        ApiDocumentationOptions options = new();
        options.MajorVersions = [];
        section.Bind(options);
        if (options.MajorVersions.Count == 0)
        {
            options.MajorVersions.Add(1);
        }

        RegisterDocuments(builder.Services, options);

        return builder;
    }

    private static void RegisterDocuments(
        IServiceCollection services,
        ApiDocumentationOptions options)
    {
        // Transformers must be registered before AddOpenApi to be resolved via DI.
        services.AddTransient<JwtBearerSecuritySchemeTransformer>();
        services.AddTransient<InternalApiDocumentTransformer>();
        services.AddTransient<TenantHeaderOperationTransformer>();
        services.AddTransient<WolverineOpenApiOperationTransformer>();
        services.AddTransient<ProblemDetailsResponseOperationTransformer>();

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
                openApiOptions.AddOperationTransformer<TenantHeaderOperationTransformer>();
                openApiOptions.AddOperationTransformer<WolverineOpenApiOperationTransformer>();
                openApiOptions.AddOperationTransformer<ProblemDetailsResponseOperationTransformer>();
            });
        }
    }
}
