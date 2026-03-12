using Granit.RateLimiting.Abstractions;
using Shouldly;
using Xunit;

namespace Granit.RateLimiting.Tests;

public sealed class RateLimitResultTests
{
    [Fact]
    public void AllowedResult_HasCorrectValues()
    {
        var result = new RateLimitResult(true, 99, 100, TimeSpan.Zero);

        result.IsAllowed.ShouldBeTrue();
        result.Remaining.ShouldBe(99);
        result.Limit.ShouldBe(100);
        result.RetryAfter.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void RejectedResult_HasCorrectValues()
    {
        var retryAfter = TimeSpan.FromSeconds(30);
        var result = new RateLimitResult(false, 0, 100, retryAfter);

        result.IsAllowed.ShouldBeFalse();
        result.Remaining.ShouldBe(0);
        result.Limit.ShouldBe(100);
        result.RetryAfter.ShouldBe(retryAfter);
    }

    [Fact]
    public void RecordEquality_Works()
    {
        var a = new RateLimitResult(true, 99, 100, TimeSpan.Zero);
        var b = new RateLimitResult(true, 99, 100, TimeSpan.Zero);

        a.ShouldBe(b);
    }
}
