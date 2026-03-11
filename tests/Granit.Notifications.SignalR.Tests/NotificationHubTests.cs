// =============================================================================
// Tests - NotificationHub
// =============================================================================
// Verifies the SignalR hub: group join on connect, group leave on disconnect,
// null UserIdentifier handling, and the [Authorize] attribute.
// =============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.SignalR.Tests;

public sealed class NotificationHubTests
{
    // -------------------------------------------------------------------------
    // Class structure
    // -------------------------------------------------------------------------

    [Fact]
    public void Class_IsSealed() =>
        typeof(NotificationHub).IsSealed.ShouldBeTrue();

    [Fact]
    public void Class_IsPublic() =>
        typeof(NotificationHub).IsPublic.ShouldBeTrue();

    [Fact]
    public void Class_ExtendsHub() =>
        typeof(NotificationHub).BaseType.ShouldBe(typeof(Hub));

    [Fact]
    public void Class_HasAuthorizeAttribute() =>
        typeof(NotificationHub).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .ShouldNotBeEmpty();

    // -------------------------------------------------------------------------
    // OnConnectedAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnConnectedAsync_WithUserId_AddsToGroup()
    {
        NotificationHub hub = new();
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.UserIdentifier.Returns("user-42");
        context.ConnectionId.Returns("conn-1");
        IGroupManager groups = Substitute.For<IGroupManager>();

        SetHubContext(hub, context, groups);

        await hub.OnConnectedAsync();

        await groups.Received(1).AddToGroupAsync("conn-1", "user-42", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnConnectedAsync_WithNullUserId_DoesNotAddToGroup()
    {
        NotificationHub hub = new();
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.UserIdentifier.Returns((string?)null);
        context.ConnectionId.Returns("conn-1");
        IGroupManager groups = Substitute.For<IGroupManager>();

        SetHubContext(hub, context, groups);

        await hub.OnConnectedAsync();

        await groups.DidNotReceive().AddToGroupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // OnDisconnectedAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnDisconnectedAsync_WithUserId_RemovesFromGroup()
    {
        NotificationHub hub = new();
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.UserIdentifier.Returns("user-42");
        context.ConnectionId.Returns("conn-1");
        IGroupManager groups = Substitute.For<IGroupManager>();

        SetHubContext(hub, context, groups);

        await hub.OnDisconnectedAsync(exception: null);

        await groups.Received(1).RemoveFromGroupAsync("conn-1", "user-42", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithNullUserId_DoesNotRemoveFromGroup()
    {
        NotificationHub hub = new();
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.UserIdentifier.Returns((string?)null);
        context.ConnectionId.Returns("conn-1");
        IGroupManager groups = Substitute.For<IGroupManager>();

        SetHubContext(hub, context, groups);

        await hub.OnDisconnectedAsync(exception: null);

        await groups.DidNotReceive().RemoveFromGroupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_StillRemovesFromGroup()
    {
        NotificationHub hub = new();
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.UserIdentifier.Returns("user-42");
        context.ConnectionId.Returns("conn-1");
        IGroupManager groups = Substitute.For<IGroupManager>();

        SetHubContext(hub, context, groups);

        await hub.OnDisconnectedAsync(new InvalidOperationException("test"));

        await groups.Received(1).RemoveFromGroupAsync("conn-1", "user-42", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void SetHubContext(Hub hub, HubCallerContext context, IGroupManager groups)
    {
        // Hub.Context and Hub.Groups are set via these properties (writable in tests).
        hub.Context = context;
        hub.Groups = groups;
        // Hub.Clients is also needed to avoid NRE in base calls.
        hub.Clients = Substitute.For<IHubCallerClients>();
    }
}
