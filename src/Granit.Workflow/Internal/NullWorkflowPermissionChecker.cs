namespace Granit.Workflow.Internal;

/// <summary>
/// Null-object implementation of <see cref="IWorkflowPermissionChecker"/> that always
/// grants permissions. Registered by default when <c>Granit.Authorization</c> is not
/// present in the container.
/// </summary>
internal sealed class NullWorkflowPermissionChecker : IWorkflowPermissionChecker
{
    /// <inheritdoc/>
    public Task<bool> IsGrantedAsync(string permissionName, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
