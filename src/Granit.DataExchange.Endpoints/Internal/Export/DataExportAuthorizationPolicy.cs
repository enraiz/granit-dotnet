using Granit.DataExchange.Endpoints.Permissions;

namespace Granit.DataExchange.Endpoints.Internal.Export;

/// <summary>
/// Authorization policy constants for data export endpoints.
/// </summary>
internal static class DataExportAuthorizationPolicy
{
    /// <summary>
    /// Name of the authorization policy that guards all data export endpoints.
    /// Equals <see cref="DataExchangePermissions.Export.Default"/> (<c>"DataExchange.Export"</c>).
    /// </summary>
    public const string PolicyName = DataExchangePermissions.Export.Default;
}
