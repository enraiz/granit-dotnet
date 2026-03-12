using Granit.Identity.Models;

namespace Granit.Identity;

/// <summary>
/// Read-only access to identity users from an external identity provider.
/// </summary>
public interface IIdentityUserReader
{
    /// <inheritdoc cref="IIdentityProvider.GetUsersAsync"/>
    Task<IReadOnlyList<IdentityUser>> GetUsersAsync(
        string? search = null,
        int? first = null,
        int? max = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.GetUserAsync"/>
    Task<IdentityUser?> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
