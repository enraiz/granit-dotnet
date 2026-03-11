using Granit.Authorization.Abstractions;
using Granit.Core.Localization;
using Granit.Settings.Endpoints.Internal;

namespace Granit.Settings.Endpoints.Permissions;

/// <summary>
/// Declares permission definitions for settings administration endpoints.
/// </summary>
internal sealed class SettingsPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(
            SettingsPermissions.GroupName,
            LocalizableString.Create<SettingsEndpointsLocalizationResource>(
                "PermissionGroup:Settings"));

        group.AddPermission(
            SettingsPermissions.GlobalRead,
            LocalizableString.Create<SettingsEndpointsLocalizationResource>(
                "Permission:Settings.Global.Read"));

        group.AddPermission(
            SettingsPermissions.GlobalManage,
            LocalizableString.Create<SettingsEndpointsLocalizationResource>(
                "Permission:Settings.Global.Manage"));

        group.AddPermission(
            SettingsPermissions.TenantRead,
            LocalizableString.Create<SettingsEndpointsLocalizationResource>(
                "Permission:Settings.Tenant.Read"));

        group.AddPermission(
            SettingsPermissions.TenantManage,
            LocalizableString.Create<SettingsEndpointsLocalizationResource>(
                "Permission:Settings.Tenant.Manage"));
    }
}
