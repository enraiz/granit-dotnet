// =============================================================================
// Tests - IdempotencyMiddleware
// =============================================================================
// 4 critical scenarios:
//   1. Double-click (InProgress) → HTTP 409 + Retry-After header
//   2. Payload mutation (Completed, wrong hash) → HTTP 422
//   3. Execution timeout → HTTP 503 + DeleteAsync called
//   4. Successful replay → X-Idempotency-Replayed: true (no business logic re-executed)
// =============================================================================

using System.Net;
using System.Net.Http.Headers;
using DigitalDynamics.Foundation.Idempotency.Abstractions;
using DigitalDynamics.Foundation.Idempotency.Attributes;
using DigitalDynamics.Foundation.Idempotency.Extensions;
using DigitalDynamics.Foundation.Idempotency.Models;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Idempotency.Tests;

public sealed class IdempotencyMiddlewareTests
{
    private const string IdempotencyKey = "test-key-abc123";
    private const string TestEndpointPath = "/orders";
    private const string TestRequestBody = """{"amount":100,"currency":"EUR"}""";

    // =========================================================================
    // Test host builder
    // =========================================================================

    private static async Task<(HttpClient Client, IHost Host)> BuildTestHostAsync(
        IIdempotencyStore store,
        Action<IdempotencyOptions>? configureOptions = null,
        RequestDelegate? endpointHandler = null)
    {
        ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns("user-42");

        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns((Guid?)null);

        IHost host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddLogging();

                    // Options — skip validator registration in tests
                    services.Configure<IdempotencyOptions>(opts =>
                    {
                        opts.ExecutionTimeout = TimeSpan.FromSeconds(25);
                        opts.InProgressTtl = TimeSpan.FromSeconds(30);
                        configureOptions?.Invoke(opts);
                    });

                    // Core idempotency services (without Redis store — mocked below)
                    services.AddSingleton<RecyclableMemoryStreamManager>();
                    services.AddTransient<Internal.IdempotencyMiddleware>();

                    // Mocked store (singleton so captured state persists across requests)
                    services.AddSingleton(store);
                    services.AddSingleton<IIdempotencyStore>(store);

                    // Mocked contextual services
                    services.AddScoped<ICurrentUserService>(_ => currentUser);
                    services.AddScoped<ICurrentTenant>(_ => currentTenant);
                });

                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseFoundationIdempotency();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints
                            .MapPost(TestEndpointPath, endpointHandler ?? DefaultEndpointHandler)
                            .WithMetadata(new IdempotentAttribute())
                            .WithName("CreateOrder");
                    });
                });
            })
            .StartAsync();

        return (host.GetTestServer().CreateClient(), host);
    }

    private static RequestDelegate DefaultEndpointHandler =>
        async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("""{"id":1,"status":"created"}""");
        };

    private static HttpRequestMessage BuildRequest(string? idempotencyKey = IdempotencyKey) =>
        new(HttpMethod.Post, TestEndpointPath)
        {
            Content = new StringContent(TestRequestBody, System.Text.Encoding.UTF8, "application/json"),
            Headers = { { "Idempotency-Key", idempotencyKey } },
        };

    // =========================================================================
    // Scenario 1: Double-click → HTTP 409 + Retry-After
    // =========================================================================

    [Fact]
    public async Task GivenKeyInProgress_WhenSameRequestArrives_Returns409WithRetryAfterHeader()
    {
        // Arrange
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();

        IdempotencyEntry inProgressEntry = new()
        {
            State = IdempotencyState.InProgress,
            PayloadHash = new string('a', 64), // any valid 64-char hex string
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // GetAsync returns InProgress entry on first call
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(Task.FromResult<IdempotencyEntry?>(inProgressEntry));

        (HttpClient client, IHost host) = await BuildTestHostAsync(store);

        try
        {
            // Act
            HttpResponseMessage response = await client.SendAsync(BuildRequest(), TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.Headers.Should().ContainKey("Retry-After");
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            body.Should().Contain("In Progress");
        }
        finally
        {
            host.Dispose();
        }
    }

    // =========================================================================
    // Scenario 2: Payload mutation → HTTP 422
    // =========================================================================

    [Fact]
    public async Task GivenCompletedEntry_WhenPayloadDiffers_Returns422()
    {
        // Arrange
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();

        // Completed entry with a deliberately wrong hash (63 zeros + "1")
        // The middleware will compute the real hash and detect the mismatch.
        IdempotencyEntry completedEntry = new()
        {
            State = IdempotencyState.Completed,
            PayloadHash = new string('0', 63) + "1",
            CreatedAt = DateTimeOffset.UtcNow,
            StatusCode = 201,
            CompletedAt = DateTimeOffset.UtcNow,
        };

        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(Task.FromResult<IdempotencyEntry?>(completedEntry));

        (HttpClient client, IHost host) = await BuildTestHostAsync(store);

        try
        {
            // Act
            HttpResponseMessage response = await client.SendAsync(BuildRequest(), TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            body.Should().Contain("Conflict");
        }
        finally
        {
            host.Dispose();
        }
    }

    // =========================================================================
    // Scenario 3: Execution timeout → HTTP 503 + DeleteAsync called
    // =========================================================================

    [Fact]
    public async Task GivenSlowHandler_WhenExecutionTimesOut_Returns503AndReleasesLock()
    {
        // Arrange
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();

        // No existing entry — lock acquisition succeeds
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(Task.FromResult<IdempotencyEntry?>(null));

        store.TryAcquireAsync(Arg.Any<string>(), Arg.Any<IdempotencyEntry>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(true));

        store.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask);

        // Endpoint that blocks until the token is cancelled (simulates a hung operation)
        RequestDelegate slowHandler = async ctx =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ctx.RequestAborted);
        };

        (HttpClient client, IHost host) = await BuildTestHostAsync(
            store,
            opts =>
            {
                opts.ExecutionTimeout = TimeSpan.FromMilliseconds(150);
                opts.InProgressTtl = TimeSpan.FromSeconds(5); // must be > ExecutionTimeout
            },
            slowHandler);

        try
        {
            // Act
            HttpResponseMessage response = await client.SendAsync(BuildRequest(), TestContext.Current.CancellationToken);

            // Assert — middleware wrote 503 and released the lock
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            await store.Received(1).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            body.Should().Contain("Timeout");
        }
        finally
        {
            host.Dispose();
        }
    }

    // =========================================================================
    // Scenario 4: Successful replay → X-Idempotency-Replayed (no re-execution)
    // =========================================================================

    [Fact]
    public async Task GivenCompletedFirstRequest_WhenRetriedWithSameKey_ReplaysWithoutExecutingBusinessLogic()
    {
        // Arrange
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();

        int handlerCallCount = 0;
        IdempotencyEntry? capturedCompleted = null;

        // First call: no entry; lock acquired; handler executes; SetCompleted captures the entry
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(callInfo => Task.FromResult<IdempotencyEntry?>(capturedCompleted));

        store.TryAcquireAsync(Arg.Any<string>(), Arg.Any<IdempotencyEntry>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(true));

        store.SetCompletedAsync(
                Arg.Any<string>(),
                Arg.Do<IdempotencyEntry>(e => capturedCompleted = e),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask);

        RequestDelegate countingHandler = async ctx =>
        {
            handlerCallCount++;
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("""{"id":99}""");
        };

        (HttpClient client, IHost host) = await BuildTestHostAsync(store, endpointHandler: countingHandler);

        try
        {
            // Act — first request (executes handler, stores completed entry)
            HttpResponseMessage first = await client.SendAsync(BuildRequest(), TestContext.Current.CancellationToken);
            first.StatusCode.Should().Be(HttpStatusCode.Created);

            // capturedCompleted is now set by the SetCompletedAsync callback;
            // second request's GetAsync will return it.
            capturedCompleted.Should().NotBeNull("SetCompletedAsync must have been called");

            // Act — second request (replay, no handler re-execution)
            HttpResponseMessage second = await client.SendAsync(BuildRequest(), TestContext.Current.CancellationToken);

            // Assert
            second.StatusCode.Should().Be(HttpStatusCode.Created);
            second.Headers.Should().ContainKey("X-Idempotency-Replayed");
            second.Headers.GetValues("X-Idempotency-Replayed").Should().Contain("true");

            handlerCallCount.Should().Be(1, "business logic must not be re-executed on replay");
        }
        finally
        {
            host.Dispose();
        }
    }
}
