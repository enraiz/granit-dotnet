using System.Threading.Channels;

namespace Granit.Notifications.Sse;

/// <summary>
/// Represents a single SSE connection for a user (one per browser tab/device).
/// </summary>
public sealed record SseConnection(
    Guid ConnectionId,
    string UserId,
    Channel<SseNotificationMessage> Channel)
{
    /// <summary>Reader side of the channel for consuming messages.</summary>
    public ChannelReader<SseNotificationMessage> Reader => Channel.Reader;
}
