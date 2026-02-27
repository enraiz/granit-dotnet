using FluentAssertions;
using Granit.Core.Exceptions;
using Granit.Features.Exceptions;
using Xunit;

namespace Granit.Features.Tests.Exceptions;

public sealed class FeatureExceptionTests
{
    // -------------------------------------------------------------------------
    // FeatureNotFoundException
    // -------------------------------------------------------------------------

    [Fact]
    public void FeatureNotFoundException_SetsFeatureName()
    {
        FeatureNotFoundException ex = new("App.Missing");

        ex.FeatureName.Should().Be("App.Missing");
    }

    [Fact]
    public void FeatureNotFoundException_MessageContainsFeatureName()
    {
        FeatureNotFoundException ex = new("App.Missing");

        ex.Message.Should().Contain("App.Missing");
    }

    [Fact]
    public void FeatureNotFoundException_InheritsFromException()
    {
        FeatureNotFoundException ex = new("App.Missing");

        ex.Should().BeAssignableTo<Exception>();
    }

    // -------------------------------------------------------------------------
    // FeatureNotEnabledException
    // -------------------------------------------------------------------------

    [Fact]
    public void FeatureNotEnabledException_SetsFeatureName()
    {
        FeatureNotEnabledException ex = new("App.Video");

        ex.FeatureName.Should().Be("App.Video");
    }

    [Fact]
    public void FeatureNotEnabledException_ErrorCode_Is_FeaturesNotEnabled()
    {
        FeatureNotEnabledException ex = new("App.Video");

        ex.ErrorCode.Should().Be("Features:NotEnabled");
    }

    [Fact]
    public void FeatureNotEnabledException_MessageContainsFeatureName()
    {
        FeatureNotEnabledException ex = new("App.Video");

        ex.Message.Should().Contain("App.Video");
    }

    [Fact]
    public void FeatureNotEnabledException_InheritsFromForbiddenException()
    {
        FeatureNotEnabledException ex = new("App.Video");

        ex.Should().BeAssignableTo<ForbiddenException>();
    }

    [Fact]
    public void FeatureNotEnabledException_ImplementsIHasErrorCode()
    {
        FeatureNotEnabledException ex = new("App.Video");

        ex.Should().BeAssignableTo<IHasErrorCode>();
    }

    // -------------------------------------------------------------------------
    // FeatureLimitExceededException
    // -------------------------------------------------------------------------

    [Fact]
    public void FeatureLimitExceededException_SetsAllProperties()
    {
        FeatureLimitExceededException ex = new("App.MaxPatients", 50, 50);

        ex.FeatureName.Should().Be("App.MaxPatients");
        ex.Current.Should().Be(50);
        ex.Limit.Should().Be(50);
    }

    [Fact]
    public void FeatureLimitExceededException_ErrorCode_Is_FeaturesLimitExceeded()
    {
        FeatureLimitExceededException ex = new("App.MaxPatients", 10, 5);

        ex.ErrorCode.Should().Be("Features:LimitExceeded");
    }

    [Fact]
    public void FeatureLimitExceededException_MessageContainsContextInfo()
    {
        FeatureLimitExceededException ex = new("App.MaxPatients", 50, 50);

        ex.Message.Should().Contain("App.MaxPatients");
        ex.Message.Should().Contain("50/50");
    }

    [Fact]
    public void FeatureLimitExceededException_InheritsFromForbiddenException()
    {
        FeatureLimitExceededException ex = new("App.MaxPatients", 1, 1);

        ex.Should().BeAssignableTo<ForbiddenException>();
    }

    // -------------------------------------------------------------------------
    // FeatureValueValidationException
    // -------------------------------------------------------------------------

    [Fact]
    public void FeatureValueValidationException_SetsAllProperties()
    {
        FeatureValueValidationException ex = new("App.MaxPatients", "abc", "must be integer");

        ex.FeatureName.Should().Be("App.MaxPatients");
        ex.InvalidValue.Should().Be("abc");
    }

    [Fact]
    public void FeatureValueValidationException_MessageContainsContext()
    {
        FeatureValueValidationException ex = new("App.MaxPatients", "abc", "must be integer");

        ex.Message.Should().Contain("App.MaxPatients");
        ex.Message.Should().Contain("abc");
        ex.Message.Should().Contain("must be integer");
    }

    [Fact]
    public void FeatureValueValidationException_InheritsFromBusinessException()
    {
        FeatureValueValidationException ex = new("App.Plan", "ultimate", "not in allowed list");

        ex.Should().BeAssignableTo<BusinessException>();
    }
}
