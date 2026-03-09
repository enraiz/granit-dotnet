using FluentValidation;
using Granit.Validation;
using Granit.Workflow.Endpoints.Dtos;

namespace Granit.Workflow.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="WorkflowTransitionRequest"/> body for workflow state transitions.
/// </summary>
/// <remarks>
/// MaxLength values must match <c>WorkflowTransitionRecordConfiguration</c>:
/// NewState = 100, Comment = 2000.
/// </remarks>
internal sealed class WorkflowTransitionRequestValidator : GranitValidator<WorkflowTransitionRequest>
{
    /// <summary>Maximum length for target state name (must match <c>WorkflowTransitionRecordConfiguration</c>).</summary>
    internal const int MaxStateLength = 100;

    /// <summary>Maximum length for transition comment (must match <c>WorkflowTransitionRecordConfiguration</c>).</summary>
    internal const int MaxCommentLength = 2000;

    public WorkflowTransitionRequestValidator()
    {
        RuleFor(x => x.TargetState)
            .NotEmpty()
            .MaximumLength(MaxStateLength);

        RuleFor(x => x.Comment)
            .MaximumLength(MaxCommentLength)
            .When(x => x.Comment is not null);
    }
}
