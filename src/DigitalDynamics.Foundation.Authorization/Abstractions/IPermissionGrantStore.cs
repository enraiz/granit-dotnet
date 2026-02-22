namespace DigitalDynamics.Foundation.Authorization.Abstractions;

/// <summary>
/// Read-only data access layer for permission grants.
/// Called by <see cref="IPermissionChecker"/> on cache miss.
/// Default implementation is <c>NullPermissionGrantStore</c> (always false).
/// Override with <c>Foundation.Authorization.EntityFrameworkCore</c> for persistence.
/// </summary>
public interface IPermissionGrantStore
{
    /// <summary>
    /// Returns true if the specified role has been explicitly granted the permission in the given tenant.
    /// </summary>
    Task<bool> IsGrantedAsync(
        string roleName,
        string permissionName,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
