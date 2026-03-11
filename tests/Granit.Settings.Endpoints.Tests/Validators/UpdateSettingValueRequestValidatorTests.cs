using FluentValidation.Results;
using Granit.Settings.Endpoints.Dtos;
using Granit.Settings.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Settings.Endpoints.Tests.Validators;

public sealed class UpdateSettingValueRequestValidatorTests
{
    private readonly UpdateSettingValueRequestValidator _validator = new();

    [Fact]
    public async Task Null_Value_Is_Valid()
    {
        UpdateSettingValueRequest request = new(null);

        ValidationResult result = await _validator.ValidateAsync(
            request, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Short_Value_Is_Valid()
    {
        UpdateSettingValueRequest request = new("fr");

        ValidationResult result = await _validator.ValidateAsync(
            request, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Value_Exceeding_MaxLength_Is_Invalid()
    {
        UpdateSettingValueRequest request = new(new string('x', UpdateSettingValueRequestValidator.MaxValueLength + 1));

        ValidationResult result = await _validator.ValidateAsync(
            request, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task Value_At_Exact_MaxLength_Is_Valid()
    {
        UpdateSettingValueRequest request = new(new string('x', UpdateSettingValueRequestValidator.MaxValueLength));

        ValidationResult result = await _validator.ValidateAsync(
            request, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Empty_String_Is_Valid()
    {
        UpdateSettingValueRequest request = new("");

        ValidationResult result = await _validator.ValidateAsync(
            request, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }
}
