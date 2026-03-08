using Granit.Authorization.Abstractions;
using Granit.Core.Localization;

namespace Granit.Templating.Endpoints.Permissions;

/// <summary>
/// Declares the <c>Templates.Manage</c> permission in the Granit RBAC system.
/// </summary>
/// <remarks>
/// Registered automatically by <see cref="GranitTemplatingEndpointsModule"/>.
/// </remarks>
internal sealed class TemplatingPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(
            TemplatingPermissions.GroupName,
            LocalizableString.Create<TemplatingEndpointsLocalizationResource>(
                "PermissionGroup:Templating"));

        group.AddPermission(
            TemplatingPermissions.Manage,
            LocalizableString.Create<TemplatingEndpointsLocalizationResource>(
                "Permission:Templates.Manage"));
    }
}
