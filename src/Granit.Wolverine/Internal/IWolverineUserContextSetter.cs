namespace Granit.Wolverine.Internal;

/// <summary>
/// Internal contract for setting the current user context in a Wolverine handler scope.
/// Implemented by <see cref="WolverineCurrentUserService"/>.
/// </summary>
public interface IWolverineUserContextSetter
{
    /// <summary>
    /// Temporarily overrides the user ID for the current async flow.
    /// Dispose the returned scope to restore the previous value.
    /// </summary>
    IDisposable Change(string? userId);
}
