namespace Granit.Wolverine.Internal;

/// <summary>
/// Internal contract for setting the current user context in a Wolverine handler scope.
/// Implemented by <see cref="WolverineCurrentUserService"/>.
/// </summary>
public interface IWolverineUserContextSetter
{
    /// <summary>
    /// Temporarily overrides the user context for the current async flow.
    /// Dispose the returned scope to restore the previous values.
    /// </summary>
    /// <param name="userId">The user ID to set.</param>
    /// <param name="firstName">The user first name (optional).</param>
    /// <param name="lastName">The user last name (optional).</param>
    IDisposable Change(string? userId, string? firstName = null, string? lastName = null);
}
