using Granit.Authorization.Abstractions;

namespace Granit.Authorization.Services;

/// <summary>
/// Default no-op implementation of <see cref="IPermissionGrantStore"/>.
/// Always returns false — all permissions are denied unless overridden by AdminRole bypass
/// or <see cref="Options.GranitAuthorizationOptions.AlwaysAllow"/>.
/// Replace with <c>Granit.Authorization.EntityFrameworkCore</c> for persistence.
/// </summary>
internal sealed class NullPermissionGrantStore : IPermissionGrantStore
{
    /// <inheritdoc />
    public Task<bool> IsGrantedAsync(
        string roleName,
        string permissionName,
        Guid? tenantId,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
