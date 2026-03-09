using FluentValidation.Results;
using Granit.Identity.Endpoints.Dtos;
using Granit.Identity.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Identity.Endpoints.Tests.Validators;

public sealed class IdentityUserCacheListRequestValidatorTests
{
    private readonly IdentityUserCacheListRequestValidator _validator = new();

    // -------------------------------------------------------------------------
    // Valid request
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_DefaultValues_ReturnsValid()
    {
        IdentityUserCacheListRequest request = new();

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithSearch_ReturnsValid()
    {
        IdentityUserCacheListRequest request = new(Search: "alice", Page: 2, PageSize: 50);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Search
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_SearchExceedsMaxLength_Fails()
    {
        string longSearch = new('x', IdentityUserCacheListRequestValidator.MaxSearchLength + 1);
        IdentityUserCacheListRequest request = new(Search: longSearch);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(IdentityUserCacheListRequest.Search));
    }

    [Fact]
    public void Validate_NullSearch_ReturnsValid()
    {
        IdentityUserCacheListRequest request = new(Search: null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Page
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageLessThanOne_Fails(int page)
    {
        IdentityUserCacheListRequest request = new(Page: page);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(IdentityUserCacheListRequest.Page));
    }

    // -------------------------------------------------------------------------
    // PageSize
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageSizeLessThanOne_Fails(int pageSize)
    {
        IdentityUserCacheListRequest request = new(PageSize: pageSize);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(IdentityUserCacheListRequest.PageSize));
    }

    [Fact]
    public void Validate_PageSizeExceedsMax_Fails()
    {
        IdentityUserCacheListRequest request = new(PageSize: IdentityUserCacheListRequestValidator.MaxPageSize + 1);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(IdentityUserCacheListRequest.PageSize));
    }
}
