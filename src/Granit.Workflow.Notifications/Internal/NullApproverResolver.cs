namespace Granit.Workflow.Notifications.Internal;

/// <summary>
/// Null-object implementation of <see cref="IApproverResolver"/> that returns
/// an empty list. Used as default when no approver resolution strategy is configured.
/// </summary>
/// <remarks>
/// When this resolver is active, approval notifications will not be sent because
/// there are no recipients. The application should register a real implementation
/// (e.g., one that queries Keycloak roles or a custom user store).
/// </remarks>
internal sealed class NullApproverResolver : IApproverResolver
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ResolveApproversAsync(
        string requiredPermission,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
