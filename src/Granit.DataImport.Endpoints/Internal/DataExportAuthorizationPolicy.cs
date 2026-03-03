using Granit.DataImport.Endpoints.Permissions;

namespace Granit.DataImport.Endpoints.Internal;

/// <summary>
/// Authorization policy constants for data export endpoints.
/// </summary>
internal static class DataExportAuthorizationPolicy
{
    /// <summary>
    /// Name of the authorization policy that guards all data export endpoints.
    /// Equals <see cref="DataImportPermissions.Export.Default"/> (<c>"DataImport.Export"</c>).
    /// </summary>
    public const string PolicyName = DataImportPermissions.Export.Default;
}
