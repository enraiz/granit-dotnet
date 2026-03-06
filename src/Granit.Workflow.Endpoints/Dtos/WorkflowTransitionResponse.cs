namespace Granit.Workflow.Endpoints.Dtos;

/// <summary>
/// Represents a single available workflow transition for the current user.
/// </summary>
/// <param name="TargetState">Target state name (e.g. "Published", "Archived").</param>
/// <param name="Name">Human-readable transition name (e.g. "Publier", "Archiver").</param>
/// <param name="Allowed">Whether the current user can trigger this transition directly.</param>
/// <param name="RequiresApproval">
/// Whether this transition will route to approval when the user lacks permission.
/// When <c>true</c> and <paramref name="Allowed"/> is <c>false</c>, the UI should
/// display "Demander l'approbation" instead of the transition name.
/// </param>
public sealed record WorkflowTransitionResponse(
    string TargetState,
    string Name,
    bool Allowed,
    bool RequiresApproval);
