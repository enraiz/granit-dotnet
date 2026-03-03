using Granit.Authorization.Abstractions;

namespace Granit.DataImport.Endpoints.Permissions;

/// <summary>
/// Declares the <c>DataImport.Admin</c> and <c>DataImport.Export</c> permissions in the Granit RBAC system.
/// </summary>
/// <remarks>
/// <para>
/// Registered automatically by <see cref="GranitDataImportEndpointsModule"/>.
/// Once registered, <c>DynamicPermissionPolicyProvider</c> creates the authorization policy
/// via <c>PermissionRequirement</c> — the full <c>IPermissionChecker</c> pipeline is used:
/// </para>
/// <list type="number">
/// <item><c>AlwaysAllow</c> (dev/test, <c>GranitAuthorizationOptions.AlwaysAllow = true</c>)</item>
/// <item>AdminRole bypass (<c>GranitAuthorizationOptions.AdminRoles</c>)</item>
/// <item>Cache + <c>IPermissionGrantStore</c> query per role</item>
/// </list>
/// <para>
/// In production, grant the permission to the desired Keycloak role via one of:
/// <list type="bullet">
/// <item>Add the role to <c>GranitAuthorizationOptions.AdminRoles</c> in <c>appsettings.json</c></item>
/// <item>Call <c>IPermissionManager.SetAsync("DataImport.Admin", "my-role", tenantId, true)</c></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DataImportPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(
            DataImportPermissions.GroupName, "Data Import");

        group.AddPermission(
            DataImportPermissions.Admin.Default,
            "Administrer les imports de données (upload, mapping, exécution, rapports)");

        group.AddPermission(
            DataImportPermissions.Export.Default,
            "Exporter des données (définitions, exécution, téléchargement, presets)");
    }
}
