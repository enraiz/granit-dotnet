using Granit.Identity.Models;

namespace Granit.Identity.Internal;

/// <summary>
/// Null-object implementation of <see cref="IIdentityProvider"/>.
/// Returns empty results for all queries and no-ops for write operations.
/// Registered by default when no provider package is installed.
/// </summary>
internal sealed class NullIdentityProvider : IIdentityProvider
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<IdentityUser>> GetUsersAsync(
        string? search = null, int? first = null, int? max = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityUser>>([]);

    /// <inheritdoc/>
    public Task<IdentityUser?> GetUserAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IdentityUser?>(null);

    /// <inheritdoc/>
    public Task SetUserEnabledAsync(
        string userId, bool enabled, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentitySession>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityDeviceActivity>>([]);

    /// <inheritdoc/>
    public Task<DateTimeOffset?> GetPasswordChangedAtAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<DateTimeOffset?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IdentityRole>> GetRolesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityRole>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IdentityUser>> GetRoleMembersAsync(
        string roleName, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityUser>>([]);
}
