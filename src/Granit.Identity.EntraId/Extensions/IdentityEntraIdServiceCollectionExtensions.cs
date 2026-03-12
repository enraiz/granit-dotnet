using Granit.Core.Diagnostics;
using Granit.Identity.EntraId.Internal;
using Granit.Identity.EntraId.Options;
using Granit.Identity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;

namespace Granit.Identity.EntraId.Extensions;

/// <summary>
/// Extension methods for registering the Entra ID identity provider.
/// </summary>
public static class IdentityEntraIdServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Microsoft Graph API as the <see cref="IIdentityProvider"/> implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires the <c>EntraIdAdmin</c> configuration section with:
    /// <c>TenantId</c>, <c>ClientId</c>, <c>ClientSecret</c>, <c>ServicePrincipalObjectId</c>.
    /// </para>
    /// <para>
    /// Required Azure AD API permissions (Application type):
    /// <list type="bullet">
    ///   <item><description><c>User.ReadWrite.All</c> — user CRUD.</description></item>
    ///   <item><description><c>Group.ReadWrite.All</c> — group membership.</description></item>
    ///   <item><description><c>AppRoleAssignment.ReadWrite.All</c> — App Role assignment.</description></item>
    ///   <item><description><c>AuditLog.Read.All</c> — sign-in activity.</description></item>
    ///   <item><description><c>Directory.ReadWrite.All</c> — password management.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitIdentityEntraId(
        this IServiceCollection services)
    {
        GranitActivitySourceRegistry.Register(Diagnostics.IdentityEntraIdActivitySource.Name);

        services.AddOptions<EntraIdAdminOptions>()
            .BindConfiguration(EntraIdAdminOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient("MicrosoftGraph", (sp, client) =>
            {
                EntraIdAdminOptions opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EntraIdAdminOptions>>().Value;
                client.BaseAddress = new Uri(opts.GraphBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            })
            .AddStandardResilienceHandler();

        services.TryAddSingleton<EntraIdAdminTokenService>();
        services.TryAddScoped<IPasswordResetNotifier, NullPasswordResetNotifier>();
        services.AddIdentityProvider<EntraIdIdentityProvider>();
        services.Replace(ServiceDescriptor.Scoped<IIdentityProviderCapabilities, EntraIdIdentityProviderCapabilities>());

        return services;
    }
}
