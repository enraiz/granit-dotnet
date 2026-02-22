using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DigitalDynamics.Foundation.ApiDocumentation.Transformers;

/// <summary>
/// Adds the JWT Bearer security scheme definition and security requirements to the OpenAPI document
/// when JWT Bearer authentication is registered in the application.
/// No-op when JWT Bearer is not configured, preventing false security indicators on public APIs.
/// </summary>
internal sealed class JwtBearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    private const string BearerSchemeId = "Bearer";

    /// <inheritdoc/>
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        IEnumerable<AuthenticationScheme> schemes =
            await authenticationSchemeProvider.GetAllSchemesAsync();

        bool hasJwtBearer = schemes.Any(s => s.Name == BearerSchemeId);
        if (!hasJwtBearer)
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[BearerSchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token. Example: \"Authorization: Bearer {token}\"",
            In = ParameterLocation.Header,
            Name = "Authorization",
        };

        OpenApiSecurityRequirement securityRequirement = new()
        {
            [new OpenApiSecuritySchemeReference(BearerSchemeId, null, null)] = [],
        };

        foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths)
        {
            foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in path.Value.Operations ?? [])
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(securityRequirement);
            }
        }
    }
}
