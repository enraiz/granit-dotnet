using FluentValidation.TestHelper;
using Granit.AI.Endpoints.Dtos;
using Granit.AI.Endpoints.Validators;
using Xunit;

namespace Granit.AI.Endpoints.Tests.Validators;

public sealed class AIWorkspaceCreateRequestValidatorTests
{
    private readonly AIWorkspaceCreateRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        AIWorkspaceCreateRequest request = new("my-workspace", "OpenAI", "gpt-4o", null, null, 0.7f, 4096);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Key_empty_fails(string? key)
    {
        AIWorkspaceCreateRequest request = new(key!, "OpenAI", "gpt-4o", null, null, null, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Key);
    }

    [Theory]
    [InlineData("MY-WORKSPACE")]
    [InlineData("workspace with spaces")]
    [InlineData("-starts-with-dash")]
    public void Key_invalid_format_fails(string key)
    {
        AIWorkspaceCreateRequest request = new(key, "OpenAI", "gpt-4o", null, null, null, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Key);
    }

    [Fact]
    public void Provider_empty_fails()
    {
        AIWorkspaceCreateRequest request = new("test", "", "gpt-4o", null, null, null, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Provider);
    }

    [Fact]
    public void Model_empty_fails()
    {
        AIWorkspaceCreateRequest request = new("test", "OpenAI", "", null, null, null, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Model);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Temperature_out_of_range_fails(float temperature)
    {
        AIWorkspaceCreateRequest request = new("test", "OpenAI", "gpt-4o", null, null, temperature, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Temperature);
    }

    [Fact]
    public void Temperature_null_passes()
    {
        AIWorkspaceCreateRequest request = new("test", "OpenAI", "gpt-4o", null, null, null, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Temperature);
    }

    [Fact]
    public void MaxOutputTokens_zero_fails()
    {
        AIWorkspaceCreateRequest request = new("test", "OpenAI", "gpt-4o", null, null, null, 0);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MaxOutputTokens);
    }

    [Fact]
    public void DisplayName_too_long_fails()
    {
        AIWorkspaceCreateRequest request = new("test", "OpenAI", "gpt-4o", new string('x', 65), null, null, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void DisplayName_null_passes()
    {
        AIWorkspaceCreateRequest request = new("test", "OpenAI", "gpt-4o", null, null, null, null);
        TestValidationResult<AIWorkspaceCreateRequest> result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }
}
