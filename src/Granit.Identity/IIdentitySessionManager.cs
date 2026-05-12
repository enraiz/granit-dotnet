using Granit.Identity.Models;

namespace Granit.Identity;

/// <summary>
/// Session management operations: list sessions, device activity, terminate sessions.
/// </summary>
public interface IIdentitySessionManager
{
    /// <summary>Lists active sessions for the specified user.</summary>
    Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the device-activity history for the specified user.</summary>
    Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Terminates the specified session for the user.</summary>
    Task TerminateSessionAsync(
        string userId,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Terminates every active session for the specified user.</summary>
    Task TerminateAllSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
