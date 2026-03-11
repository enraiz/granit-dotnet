using FluentValidation.Results;
using Granit.Authentication.ApiKeys.Endpoints.Dtos;
using Granit.Authentication.ApiKeys.Endpoints.Validators;
using Granit.Timing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Endpoints.Tests.Validators;

public sealed class ApiKeyCreateRequestValidatorTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly ApiKeyCreateRequestValidator _validator;

    public ApiKeyCreateRequestValidatorTests()
    {
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(FixedNow);
        _validator = new ApiKeyCreateRequestValidator(clock);
    }

    [Fact]
    public void Valid_Request_Passes()
    {
        var request = new ApiKeyCreateRequest("My Key", ApiKeyType.Secret, "live");
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Valid_Request_With_All_Fields_Passes()
    {
        var request = new ApiKeyCreateRequest(
            "My Key",
            ApiKeyType.Secret,
            "live",
            ["MyApp.Patients.Read", "MyApp.Patients.Write"],
            ["10.0.0.0/24"],
            FixedNow.AddDays(30),
            CacheBehavior.NoCache);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Name_Fails(string? name)
    {
        var request = new ApiKeyCreateRequest(name!, ApiKeyType.Secret, "live");
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Too_Long_Fails()
    {
        var request = new ApiKeyCreateRequest(
            new string('A', ApiKeyCreateRequestValidator.MaxNameLength + 1),
            ApiKeyType.Secret, "live");
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("staging")]
    [InlineData("production")]
    public void Invalid_Environment_Fails(string environment)
    {
        var request = new ApiKeyCreateRequest("Key", ApiKeyType.Secret, environment);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Environment");
    }

    [Theory]
    [InlineData("live")]
    [InlineData("test")]
    [InlineData("dev")]
    public void Valid_Environment_Passes(string environment)
    {
        var request = new ApiKeyCreateRequest("Key", ApiKeyType.Secret, environment);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Invalid_Cidr_Fails()
    {
        var request = new ApiKeyCreateRequest(
            "Key", ApiKeyType.Secret, "live",
            AllowedCidrs: ["not-a-cidr"]);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Valid_Cidr_Passes()
    {
        var request = new ApiKeyCreateRequest(
            "Key", ApiKeyType.Secret, "live",
            AllowedCidrs: ["192.168.1.0/24", "10.0.0.0/8"]);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Past_ExpiresAt_Fails()
    {
        var request = new ApiKeyCreateRequest(
            "Key", ApiKeyType.Secret, "live",
            ExpiresAt: FixedNow.AddDays(-1));
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "ExpiresAt");
    }

    [Fact]
    public void Too_Many_Permissions_Fails()
    {
        var permissions = Enumerable.Range(0, ApiKeyCreateRequestValidator.MaxPermissions + 1)
            .Select(i => $"Permission.{i}")
            .ToList();

        var request = new ApiKeyCreateRequest(
            "Key", ApiKeyType.Secret, "live",
            Permissions: permissions);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }
}
