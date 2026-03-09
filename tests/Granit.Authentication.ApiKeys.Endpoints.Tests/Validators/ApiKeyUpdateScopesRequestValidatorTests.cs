using FluentValidation.Results;
using Granit.Authentication.ApiKeys.Endpoints.Dtos;
using Granit.Authentication.ApiKeys.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Endpoints.Tests.Validators;

public sealed class ApiKeyUpdateScopesRequestValidatorTests
{
    private readonly ApiKeyUpdateScopesRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Passes()
    {
        var request = new ApiKeyUpdateScopesRequest(
            ["Permission.Read"], ["10.0.0.0/24"]);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_Lists_Pass()
    {
        var request = new ApiKeyUpdateScopesRequest([], []);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Invalid_Cidr_Fails()
    {
        var request = new ApiKeyUpdateScopesRequest([], ["not-valid"]);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Too_Many_Permissions_Fails()
    {
        var permissions = Enumerable.Range(0, ApiKeyUpdateScopesRequestValidator.MaxPermissions + 1)
            .Select(i => $"Permission.{i}")
            .ToList();
        var request = new ApiKeyUpdateScopesRequest(permissions, []);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Too_Many_Cidrs_Fails()
    {
        var cidrs = Enumerable.Range(0, ApiKeyUpdateScopesRequestValidator.MaxCidrs + 1)
            .Select(i => $"10.0.{i % 256}.0/24")
            .ToList();
        var request = new ApiKeyUpdateScopesRequest([], cidrs);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_Permission_Entry_Fails()
    {
        var request = new ApiKeyUpdateScopesRequest(["Valid", ""], []);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }
}
