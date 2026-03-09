using FluentValidation.Results;
using Granit.Identity.Endpoints.Dtos;
using Granit.Identity.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Identity.Endpoints.Tests.Validators;

public sealed class IdentityUserCacheBatchRequestValidatorTests
{
    private readonly IdentityUserCacheBatchRequestValidator _validator = new();

    // -------------------------------------------------------------------------
    // Valid request
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ValidRequest_ReturnsValid()
    {
        IdentityUserCacheBatchRequest request = new(["user-1", "user-2"]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // UserIds — empty collection
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyUserIds_Fails()
    {
        IdentityUserCacheBatchRequest request = new([]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(IdentityUserCacheBatchRequest.UserIds));
    }

    // -------------------------------------------------------------------------
    // UserIds — exceeds max batch size
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ExceedsMaxBatchSize_Fails()
    {
        List<string> ids = Enumerable.Range(1, IdentityUserCacheBatchRequestValidator.MaxBatchSize + 1)
            .Select(i => $"user-{i}")
            .ToList();
        IdentityUserCacheBatchRequest request = new(ids);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_ExactMaxBatchSize_ReturnsValid()
    {
        List<string> ids = Enumerable.Range(1, IdentityUserCacheBatchRequestValidator.MaxBatchSize)
            .Select(i => $"user-{i}")
            .ToList();
        IdentityUserCacheBatchRequest request = new(ids);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // UserIds — individual item validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        IdentityUserCacheBatchRequest request = new(["user-1", ""]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_UserIdExceedsMaxLength_Fails()
    {
        string longId = new('x', IdentityUserCacheBatchRequestValidator.MaxUserIdLength + 1);
        IdentityUserCacheBatchRequest request = new([longId]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }
}
