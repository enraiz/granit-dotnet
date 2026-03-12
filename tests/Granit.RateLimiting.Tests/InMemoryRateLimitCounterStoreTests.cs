using Granit.RateLimiting.Abstractions;
using Granit.RateLimiting.Internal;
using Granit.RateLimiting.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace Granit.RateLimiting.Tests;

public sealed class InMemoryRateLimitCounterStoreTests
{
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);
    private readonly InMemoryRateLimitCounterStore _store;
    private readonly RateLimitPolicyOptions _defaultPolicy = new() { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) };

    public InMemoryRateLimitCounterStoreTests()
    {
        _store = new InMemoryRateLimitCounterStore(_timeProvider);
    }

    // =========================================================================
    // Sliding Window
    // =========================================================================

    [Fact]
    public async Task SlidingWindow_FirstRequest_IsAllowed()
    {
        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.SlidingWindow,
            _defaultPolicy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Remaining.ShouldBe(4);
        result.Limit.ShouldBe(5);
    }

    [Fact]
    public async Task SlidingWindow_ExceedsLimit_IsRejected()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.CheckAndIncrementAsync(
                "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.SlidingWindow,
                _defaultPolicy, TestContext.Current.CancellationToken);
        }

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.SlidingWindow,
            _defaultPolicy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Remaining.ShouldBe(0);
        result.RetryAfter.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task SlidingWindow_WindowExpires_AllowsAgain()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.CheckAndIncrementAsync(
                "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.SlidingWindow,
                _defaultPolicy, TestContext.Current.CancellationToken);
        }

        _timeProvider.Advance(TimeSpan.FromMinutes(2));

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.SlidingWindow,
            _defaultPolicy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task SlidingWindow_DifferentKeys_TrackSeparately()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.CheckAndIncrementAsync(
                "key1", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.SlidingWindow,
                _defaultPolicy, TestContext.Current.CancellationToken);
        }

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key2", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.SlidingWindow,
            _defaultPolicy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Remaining.ShouldBe(4);
    }

    // =========================================================================
    // Fixed Window
    // =========================================================================

    [Fact]
    public async Task FixedWindow_FirstRequest_IsAllowed()
    {
        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.FixedWindow,
            _defaultPolicy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Remaining.ShouldBe(4);
    }

    [Fact]
    public async Task FixedWindow_ExceedsLimit_IsRejected()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.CheckAndIncrementAsync(
                "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.FixedWindow,
                _defaultPolicy, TestContext.Current.CancellationToken);
        }

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.FixedWindow,
            _defaultPolicy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Remaining.ShouldBe(0);
        result.RetryAfter.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task FixedWindow_NewWindow_ResetsCounter()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.CheckAndIncrementAsync(
                "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.FixedWindow,
                _defaultPolicy, TestContext.Current.CancellationToken);
        }

        _timeProvider.Advance(TimeSpan.FromMinutes(2));

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.FixedWindow,
            _defaultPolicy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Remaining.ShouldBe(4);
    }

    // =========================================================================
    // Token Bucket
    // =========================================================================

    [Fact]
    public async Task TokenBucket_FirstRequest_IsAllowed()
    {
        var policy = new RateLimitPolicyOptions
        {
            Algorithm = RateLimitAlgorithm.TokenBucket,
            TokenLimit = 10,
            TokensPerPeriod = 2,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
        };

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 10, TimeSpan.FromMinutes(1), RateLimitAlgorithm.TokenBucket,
            policy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Remaining.ShouldBe(9);
        result.Limit.ShouldBe(10);
    }

    [Fact]
    public async Task TokenBucket_EmptyBucket_IsRejected()
    {
        var policy = new RateLimitPolicyOptions
        {
            Algorithm = RateLimitAlgorithm.TokenBucket,
            TokenLimit = 3,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
        };

        for (int i = 0; i < 3; i++)
        {
            await _store.CheckAndIncrementAsync(
                "key", 3, TimeSpan.FromMinutes(1), RateLimitAlgorithm.TokenBucket,
                policy, TestContext.Current.CancellationToken);
        }

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 3, TimeSpan.FromMinutes(1), RateLimitAlgorithm.TokenBucket,
            policy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Remaining.ShouldBe(0);
    }

    [Fact]
    public async Task TokenBucket_ReplenishesAfterPeriod()
    {
        var policy = new RateLimitPolicyOptions
        {
            Algorithm = RateLimitAlgorithm.TokenBucket,
            TokenLimit = 3,
            TokensPerPeriod = 2,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
        };

        for (int i = 0; i < 3; i++)
        {
            await _store.CheckAndIncrementAsync(
                "key", 3, TimeSpan.FromMinutes(1), RateLimitAlgorithm.TokenBucket,
                policy, TestContext.Current.CancellationToken);
        }

        _timeProvider.Advance(TimeSpan.FromSeconds(10));

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 3, TimeSpan.FromMinutes(1), RateLimitAlgorithm.TokenBucket,
            policy, TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Remaining.ShouldBe(1); // 0 + 2 replenished - 1 consumed = 1
    }

    [Fact]
    public async Task TokenBucket_DoesNotExceedMaxTokens()
    {
        var policy = new RateLimitPolicyOptions
        {
            Algorithm = RateLimitAlgorithm.TokenBucket,
            TokenLimit = 5,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        };

        // Initial request consumes 1 token
        await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.TokenBucket,
            policy, TestContext.Current.CancellationToken);

        // Wait for replenishment — tokens should be capped at TokenLimit
        _timeProvider.Advance(TimeSpan.FromSeconds(5));

        RateLimitResult result = await _store.CheckAndIncrementAsync(
            "key", 5, TimeSpan.FromMinutes(1), RateLimitAlgorithm.TokenBucket,
            policy, TestContext.Current.CancellationToken);

        result.Remaining.ShouldBeLessThanOrEqualTo(policy.TokenLimit - 1);
    }
}
