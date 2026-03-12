using Granit.Identity.Models;

namespace Granit.Identity;

/// <summary>
/// Session management operations: list sessions, device activity, terminate sessions.
/// </summary>
public interface IIdentitySessionManager
{
    /// <inheritdoc cref="IIdentityProvider.GetUserSessionsAsync"/>
    Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.GetUserDeviceActivityAsync"/>
    Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.TerminateSessionAsync"/>
    Task TerminateSessionAsync(
        string userId,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.TerminateAllSessionsAsync"/>
    Task TerminateAllSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
