using Granit.ApiDocumentation.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Granit.ApiDocumentation.Transformers;

/// <summary>
/// Replaces the HTTP Bearer security scheme with an OAuth2 Authorization Code flow
/// when <see cref="OAuth2Options.IsConfigured"/> is <c>true</c>.
/// Runs after <see cref="JwtBearerSecuritySchemeTransformer"/> so the Bearer scheme
/// is already present when this transformer executes.
/// No-op when OAuth2 is not configured or JWT Bearer is not registered.
/// </summary>
internal sealed class OAuth2SecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IOptions<ApiDocumentationOptions> apiDocOptions) : IOpenApiDocumentTransformer
{
    private const string BearerSchemeId = "Bearer";
    private const string OAuth2SchemeId = "OAuth2";

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

        OAuth2Options oauth2 = apiDocOptions.Value.OAuth2;
        if (!oauth2.IsConfigured)
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        // Remove Bearer scheme (replaced by OAuth2)
        document.Components.SecuritySchemes.Remove(BearerSchemeId);

        // Add OAuth2 Authorization Code scheme
        Dictionary<string, string> scopes = [];
        foreach (string scope in oauth2.Scopes)
        {
            scopes[scope] = scope;
        }

        document.Components.SecuritySchemes[OAuth2SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "OAuth2 Authorization Code flow with PKCE",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(oauth2.AuthorizationUrl!),
                    TokenUrl = new Uri(oauth2.TokenUrl!),
                    Scopes = scopes,
                },
            },
        };

        // Replace security requirements on all operations
        OpenApiSecurityRequirement oAuth2Requirement = new()
        {
            [new OpenApiSecuritySchemeReference(OAuth2SchemeId, null, null)] =
                [.. oauth2.Scopes],
        };

        foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths)
        {
            if (path.Value.Operations is null)
            {
                continue;
            }

            foreach (OpenApiOperation operation in path.Value.Operations.Select(op => op.Value))
            {
                if (operation.Security is null)
                {
                    continue;
                }

                // Replace all existing security requirements with OAuth2
                operation.Security.Clear();
                operation.Security.Add(oAuth2Requirement);
            }
        }
    }
}
