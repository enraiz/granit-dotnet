using Granit.RateLimiting.Exceptions;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace Granit.RateLimiting.Tests;

public sealed class RateLimitExceptionTests
{
    [Fact]
    public void RateLimitExceededException_SetsProperties()
    {
        var retryAfter = TimeSpan.FromSeconds(42);

        var ex = new RateLimitExceededException("api", retryAfter, 100, 0);

        ex.PolicyName.ShouldBe("api");
        ex.RetryAfter.ShouldBe(retryAfter);
        ex.Limit.ShouldBe(100);
        ex.Remaining.ShouldBe(0);
        ex.Message.ShouldContain("api");
        ex.Message.ShouldContain("42");
    }

    [Fact]
    public void StatusCodeMapper_MapsRateLimitExceededException_To429()
    {
        var mapper = new RateLimitExceptionStatusCodeMapper();
        var ex = new RateLimitExceededException("api", TimeSpan.FromSeconds(1), 100, 0);

        int? statusCode = mapper.TryGetStatusCode(ex);

        statusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public void StatusCodeMapper_ReturnsNull_ForOtherExceptions()
    {
        var mapper = new RateLimitExceptionStatusCodeMapper();

        int? statusCode = mapper.TryGetStatusCode(new InvalidOperationException());

        statusCode.ShouldBeNull();
    }
}
