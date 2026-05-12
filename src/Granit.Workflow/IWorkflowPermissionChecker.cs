namespace Granit.Workflow;

/// <summary>
/// Abstraction for checking whether the current user has a specific permission
/// in the context of a workflow transition.
/// </summary>
/// <remarks>
/// <para>
/// This interface decouples <c>Granit.Workflow</c> from <c>Granit.Authorization</c>.
/// When <c>Granit.Authorization</c> is registered, a bridge implementation delegates
/// to <c>Granit.Authorization.IPermissionChecker</c>.
/// </para>
/// <para>
/// The default registration is <see cref="Internal.NullWorkflowPermissionChecker"/>
/// which always returns <c>true</c> — suitable for development/testing environments
/// where authorization is not configured.
/// </para>
/// </remarks>
public interface IWorkflowPermissionChecker
{
    /// <summary>
    /// Returns <c>true</c> if the current user has the specified permission.
    /// </summary>
    Task<bool> IsGrantedAsync(string permissionName, CancellationToken cancellationToken = default);
}
