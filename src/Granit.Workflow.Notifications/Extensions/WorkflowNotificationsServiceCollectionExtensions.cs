using Granit.Workflow.Notifications.Internal;
using Granit.Workflow.Notifications.Keycloak;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Workflow.Notifications.Extensions;

/// <summary>
/// Extension methods for registering Granit.Workflow.Notifications services.
/// </summary>
public static class WorkflowNotificationsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the workflow approval notification bridge services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers a <see cref="NullApproverResolver"/> as the default
    /// <see cref="IApproverResolver"/>. The host application should replace this
    /// with a real implementation that queries the user/role store, or call
    /// <see cref="AddKeycloakApproverResolver"/> for the built-in Keycloak integration.
    /// </para>
    /// <para>
    /// The <see cref="Handlers.WorkflowApprovalRequestedHandler"/> is discovered
    /// automatically by Wolverine handler scanning.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitWorkflowNotifications(
        this IServiceCollection services)
    {
        services.TryAddScoped<IApproverResolver, NullApproverResolver>();
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="IApproverResolver"/> implementation for
    /// resolving users authorized to approve workflow transitions.
    /// </summary>
    /// <typeparam name="TResolver">The approver resolver implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowApproverResolver<TResolver>(
        this IServiceCollection services)
        where TResolver : class, IApproverResolver
    {
        services.Replace(ServiceDescriptor.Scoped<IApproverResolver, TResolver>());
        return services;
    }

    /// <summary>
    /// Registers the built-in Keycloak Admin API approver resolver.
    /// Resolves approvers by: permission → roles (via <c>IPermissionManager</c>) →
    /// Keycloak realm role members (via Admin API).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires the <c>KeycloakAdmin</c> configuration section with:
    /// <c>BaseUrl</c>, <c>Realm</c>, <c>ClientId</c>, <c>ClientSecret</c>.
    /// The service account must have the <c>realm-management:view-users</c> role.
    /// </para>
    /// <para>
    /// Also requires <c>Granit.Authorization</c> to be registered (provides
    /// <c>IPermissionManager</c> for the permission → role lookup).
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeycloakApproverResolver(
        this IServiceCollection services)
    {
        services.AddOptions<KeycloakAdminOptions>()
            .BindConfiguration(KeycloakAdminOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient("KeycloakAdmin");
        services.TryAddSingleton<KeycloakAdminTokenService>();
        services.AddWorkflowApproverResolver<KeycloakApproverResolver>();

        return services;
    }
}
