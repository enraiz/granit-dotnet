namespace DigitalDynamics.Foundation.Authorization.Abstractions;

/// <summary>
/// Administrative service for managing role → permission grants.
/// Each call to <see cref="SetAsync"/> emits an HDS audit log entry and invalidates the cache.
/// Available only when <c>Foundation.Authorization.EntityFrameworkCore</c> is registered.
/// </summary>
public interface IPermissionManager
{
    /// <summary>
    /// Grants or revokes a permission for a role within a tenant.
    /// No-op if the current state already matches <paramref name="isGranted"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="permissionName"/> has not been declared.
    /// </exception>
    Task SetAsync(
        string permissionName,
        string roleName,
        Guid? tenantId,
        bool isGranted,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true if the role has been explicitly granted the permission.</summary>
    Task<bool> IsGrantedAsync(
        string permissionName,
        string roleName,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all permission names explicitly granted to the role in the given tenant.</summary>
    Task<IReadOnlyList<string>> GetGrantedPermissionsAsync(
        string roleName,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all role names that have been explicitly granted the permission in the given tenant.</summary>
    Task<IReadOnlyList<string>> GetGrantedRolesAsync(
        string permissionName,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
