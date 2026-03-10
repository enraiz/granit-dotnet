using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Tests;

public sealed class ApiKeyOptionsTests
{
    [Fact]
    public void CacheDuration_DefaultsToFiveMinutes()
    {
        var options = new ApiKeyOptions();

        options.CacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void TrackLastUsed_DefaultsToTrue()
    {
        var options = new ApiKeyOptions();

        options.TrackLastUsed.ShouldBeTrue();
    }

    [Fact]
    public void CacheDuration_CanBeSetToZero()
    {
        var options = new ApiKeyOptions { CacheDuration = TimeSpan.Zero };

        options.CacheDuration.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void TrackLastUsed_CanBeDisabled()
    {
        var options = new ApiKeyOptions { TrackLastUsed = false };

        options.TrackLastUsed.ShouldBeFalse();
    }

    [Fact]
    public void CacheDuration_CanBeSetToCustomValue()
    {
        var duration = TimeSpan.FromMinutes(30);
        var options = new ApiKeyOptions { CacheDuration = duration };

        options.CacheDuration.ShouldBe(duration);
    }
}
