using FluentAssertions;
using Granit.Features.Checker;
using Granit.Features.Exceptions;
using Granit.Features.Limits;
using NSubstitute;
using Xunit;

namespace Granit.Features.Tests.Limits;

public sealed class FeatureLimitGuardTests
{
    private static FeatureLimitGuard BuildGuard(long resolvedLimit)
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        checker.GetNumericAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedLimit);
        return new FeatureLimitGuard(checker);
    }

    [Fact]
    public async Task CheckAsync_BelowLimit_DoesNotThrow()
    {
        FeatureLimitGuard guard = BuildGuard(resolvedLimit: 100);

        Func<Task> act = () => guard.CheckAsync("App.MaxPatients", currentCount: 50,
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CheckAsync_AtLimit_Throws_FeatureLimitExceededException()
    {
        FeatureLimitGuard guard = BuildGuard(resolvedLimit: 50);

        Func<Task> act = () => guard.CheckAsync("App.MaxPatients", currentCount: 50,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FeatureLimitExceededException>()
            .WithMessage("*App.MaxPatients*");
    }

    [Fact]
    public async Task CheckAsync_AboveLimit_Throws_FeatureLimitExceededException()
    {
        FeatureLimitGuard guard = BuildGuard(resolvedLimit: 50);

        Func<Task> act = () => guard.CheckAsync("App.MaxPatients", currentCount: 99,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FeatureLimitExceededException>();
    }

    [Fact]
    public async Task GetLimitAsync_Returns_ResolvedNumericValue()
    {
        FeatureLimitGuard guard = BuildGuard(resolvedLimit: 200);

        long limit = await guard.GetLimitAsync("App.MaxPatients", TestContext.Current.CancellationToken);

        limit.Should().Be(200);
    }

    [Fact]
    public async Task FeatureLimitExceededException_Carries_Context()
    {
        FeatureLimitGuard guard = BuildGuard(resolvedLimit: 10);

        FeatureLimitExceededException? ex = null;
        try
        {
            await guard.CheckAsync("App.MaxPatients", currentCount: 10,
                TestContext.Current.CancellationToken);
        }
        catch (FeatureLimitExceededException caught)
        {
            ex = caught;
        }

        ex.Should().NotBeNull();
        ex!.FeatureName.Should().Be("App.MaxPatients");
        ex.Current.Should().Be(10);
        ex.Limit.Should().Be(10);
        ex.ErrorCode.Should().Be("Features:LimitExceeded");
    }
}
