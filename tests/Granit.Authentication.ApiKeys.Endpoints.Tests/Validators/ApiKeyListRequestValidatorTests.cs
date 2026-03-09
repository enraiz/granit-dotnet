using FluentValidation.Results;
using Granit.Authentication.ApiKeys.Endpoints.Dtos;
using Granit.Authentication.ApiKeys.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Endpoints.Tests.Validators;

public sealed class ApiKeyListRequestValidatorTests
{
    private readonly ApiKeyListRequestValidator _validator = new();

    [Fact]
    public void Default_Request_Passes()
    {
        var request = new ApiKeyListRequest();
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Search_Too_Long_Fails()
    {
        var request = new ApiKeyListRequest(
            Search: new string('A', ApiKeyListRequestValidator.MaxSearchLength + 1));
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Search");
    }

    [Fact]
    public void Page_Zero_Fails()
    {
        var request = new ApiKeyListRequest(Page: 0);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Page");
    }

    [Fact]
    public void PageSize_Over_Max_Fails()
    {
        var request = new ApiKeyListRequest(
            PageSize: ApiKeyListRequestValidator.MaxPageSize + 1);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void PageSize_Zero_Fails()
    {
        var request = new ApiKeyListRequest(PageSize: 0);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Valid_PageSize_Passes(int pageSize)
    {
        var request = new ApiKeyListRequest(PageSize: pageSize);
        ValidationResult result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }
}
