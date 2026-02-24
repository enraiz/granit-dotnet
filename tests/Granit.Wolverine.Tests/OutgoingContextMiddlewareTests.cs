// =============================================================================
// Tests - OutgoingContextMiddleware
// =============================================================================
// Verifies that X-Tenant-Id and X-User-Id headers are correctly injected into
// outgoing Wolverine envelopes according to the current tenant/user context.
// =============================================================================

using FluentAssertions;
using Granit.Core.MultiTenancy;
using Granit.Security;
using Granit.Wolverine.Middleware;
using NSubstitute;
using Wolverine;
using Xunit;

namespace Granit.Wolverine.Tests;

public sealed class OutgoingContextMiddlewareTests
{
    private static Envelope CreateEnvelope() => new();

    [Fact]
    public void Before_WithTenantAndUser_SetsBothHeaders()
    {
        Guid tenantId = Guid.NewGuid();
        const string userId = "user-123";

        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(tenantId);

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.IsAuthenticated.Returns(true);
        userService.UserId.Returns(userId);

        OutgoingContextMiddleware middleware = new(tenant, userService);
        Envelope envelope = CreateEnvelope();

        middleware.Before(envelope);

        envelope.Headers[OutgoingContextMiddleware.TenantIdHeader].Should().Be(tenantId.ToString());
        envelope.Headers[OutgoingContextMiddleware.UserIdHeader].Should().Be(userId);
    }

    [Fact]
    public void Before_WithTenantOnly_SetsTenantHeaderOnly()
    {
        Guid tenantId = Guid.NewGuid();

        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(tenantId);

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.IsAuthenticated.Returns(false);

        OutgoingContextMiddleware middleware = new(tenant, userService);
        Envelope envelope = CreateEnvelope();

        middleware.Before(envelope);

        envelope.Headers[OutgoingContextMiddleware.TenantIdHeader].Should().Be(tenantId.ToString());
        envelope.Headers.ContainsKey(OutgoingContextMiddleware.UserIdHeader).Should().BeFalse();
    }

    [Fact]
    public void Before_WithUserOnly_SetsUserHeaderOnly()
    {
        const string userId = "user-456";

        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns((Guid?)null);

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.IsAuthenticated.Returns(true);
        userService.UserId.Returns(userId);

        OutgoingContextMiddleware middleware = new(tenant, userService);
        Envelope envelope = CreateEnvelope();

        middleware.Before(envelope);

        envelope.Headers.ContainsKey(OutgoingContextMiddleware.TenantIdHeader).Should().BeFalse();
        envelope.Headers[OutgoingContextMiddleware.UserIdHeader].Should().Be(userId);
    }

    [Fact]
    public void Before_WithNoTenantAndNoUser_SetsNoHeaders()
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns((Guid?)null);

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.IsAuthenticated.Returns(false);

        OutgoingContextMiddleware middleware = new(tenant, userService);
        Envelope envelope = CreateEnvelope();

        middleware.Before(envelope);

        envelope.Headers.ContainsKey(OutgoingContextMiddleware.TenantIdHeader).Should().BeFalse();
        envelope.Headers.ContainsKey(OutgoingContextMiddleware.UserIdHeader).Should().BeFalse();
    }

    [Fact]
    public void Before_WithAuthenticatedUserButNullUserId_DoesNotSetUserHeader()
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns((Guid?)null);

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.IsAuthenticated.Returns(true);
        userService.UserId.Returns((string?)null);

        OutgoingContextMiddleware middleware = new(tenant, userService);
        Envelope envelope = CreateEnvelope();

        middleware.Before(envelope);

        envelope.Headers.ContainsKey(OutgoingContextMiddleware.UserIdHeader).Should().BeFalse();
    }

    [Fact]
    public void Before_WithAuthenticatedUserButEmptyUserId_DoesNotSetUserHeader()
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns((Guid?)null);

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.IsAuthenticated.Returns(true);
        userService.UserId.Returns(string.Empty);

        OutgoingContextMiddleware middleware = new(tenant, userService);
        Envelope envelope = CreateEnvelope();

        middleware.Before(envelope);

        envelope.Headers.ContainsKey(OutgoingContextMiddleware.UserIdHeader).Should().BeFalse();
    }
}
