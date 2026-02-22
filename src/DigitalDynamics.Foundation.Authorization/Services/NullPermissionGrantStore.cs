using DigitalDynamics.Foundation.Authorization.Abstractions;

namespace DigitalDynamics.Foundation.Authorization.Services;

/// <summary>
/// Default no-op implementation of <see cref="IPermissionGrantStore"/>.
/// Always returns false — all permissions are denied unless overridden by AdminRole bypass
/// or <see cref="Options.FoundationAuthorizationOptions.AlwaysAllow"/>.
/// Replace with <c>Foundation.Authorization.EntityFrameworkCore</c> for persistence.
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
