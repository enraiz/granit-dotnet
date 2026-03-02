using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Granit.Notifications.SignalR;

/// <summary>
/// SignalR hub for real-time notification delivery. Clients are grouped by userId.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    /// <inheritdoc/>
    public override async Task OnConnectedAsync()
    {
        string? userId = Context.UserIdentifier;
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = Context.UserIdentifier;
        if (userId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId).ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
