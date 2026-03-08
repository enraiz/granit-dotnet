namespace Granit.Notifications.Zulip;

/// <summary>Abstraction for sending messages to Zulip.</summary>
public interface IZulipSender
{
    /// <summary>Sends a message to Zulip.</summary>
    Task SendAsync(ZulipMessage message, CancellationToken cancellationToken = default);
}
