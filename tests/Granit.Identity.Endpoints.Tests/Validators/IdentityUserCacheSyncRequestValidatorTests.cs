using FluentValidation.Results;
using Granit.Identity.Endpoints.Dtos;
using Granit.Identity.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Identity.Endpoints.Tests.Validators;

public sealed class IdentityUserCacheSyncRequestValidatorTests
{
    private readonly IdentityUserCacheSyncRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsValid()
    {
        IdentityUserCacheSyncRequest request = new(["user-1"]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyUserIds_Fails()
    {
        IdentityUserCacheSyncRequest request = new([]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(IdentityUserCacheSyncRequest.UserIds));
    }

    [Fact]
    public void Validate_ExceedsMaxBatchSize_Fails()
    {
        var ids = Enumerable.Range(1, IdentityUserCacheSyncRequestValidator.MaxBatchSize + 1)
            .Select(i => $"user-{i}")
            .ToList();
        IdentityUserCacheSyncRequest request = new(ids);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        IdentityUserCacheSyncRequest request = new([""]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_UserIdExceedsMaxLength_Fails()
    {
        string longId = new('x', IdentityUserCacheSyncRequestValidator.MaxUserIdLength + 1);
        IdentityUserCacheSyncRequest request = new([longId]);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }
}
