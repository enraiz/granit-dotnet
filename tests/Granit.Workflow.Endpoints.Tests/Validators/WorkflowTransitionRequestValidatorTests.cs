using FluentValidation.Results;
using Granit.Workflow.Endpoints.Dtos;
using Granit.Workflow.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Endpoints.Tests.Validators;

public sealed class WorkflowTransitionRequestValidatorTests
{
    private readonly WorkflowTransitionRequestValidator _validator = new();

    // -------------------------------------------------------------------------
    // Valid request
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ValidRequest_ReturnsValid()
    {
        WorkflowTransitionRequest request = new(TargetState: "Published", Comment: "Approved by manager.");

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullComment_ReturnsValid()
    {
        WorkflowTransitionRequest request = new(TargetState: "Draft", Comment: null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // TargetState
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyTargetState_Fails(string? targetState)
    {
        WorkflowTransitionRequest request = new(TargetState: targetState!, Comment: null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(WorkflowTransitionRequest.TargetState));
    }

    [Fact]
    public void Validate_TargetStateExceedsMaxLength_Fails()
    {
        string longState = new('x', WorkflowTransitionRequestValidator.MaxStateLength + 1);
        WorkflowTransitionRequest request = new(TargetState: longState, Comment: null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(WorkflowTransitionRequest.TargetState));
    }

    // -------------------------------------------------------------------------
    // Comment
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_CommentExceedsMaxLength_Fails()
    {
        string longComment = new('x', WorkflowTransitionRequestValidator.MaxCommentLength + 1);
        WorkflowTransitionRequest request = new(TargetState: "Published", Comment: longComment);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(WorkflowTransitionRequest.Comment));
    }

    [Fact]
    public void Validate_CommentAtMaxLength_ReturnsValid()
    {
        string maxComment = new('x', WorkflowTransitionRequestValidator.MaxCommentLength);
        WorkflowTransitionRequest request = new(TargetState: "Published", Comment: maxComment);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }
}
