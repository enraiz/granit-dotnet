using Granit.Notifications.Abstractions;
using Granit.Notifications.Zulip.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Zulip;

/// <summary>Zulip notification channel that sends notifications as messages to Zulip streams.</summary>
internal sealed partial class ZulipNotificationChannel(
    IZulipSender sender,
    IOptions<ZulipChannelOptions> options,
    ILogger<ZulipNotificationChannel> logger) : INotificationChannel
{
    /// <inheritdoc />
    public string Name => NotificationChannels.Zulip;

    /// <inheritdoc />
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken cancellationToken = default)
    {
        ZulipChannelOptions channelOptions = options.Value;

        string content = $"**{context.NotificationTypeName}** ({context.Severity})\n\nNotification: {context.NotificationTypeName}";

        await sender.SendAsync(new ZulipMessage
        {
            Type = "stream",
            Stream = channelOptions.DefaultStream,
            Topic = channelOptions.DefaultTopic,
            Content = content,
        }, cancellationToken).ConfigureAwait(false);

        LogMessageSent(channelOptions.DefaultStream, channelOptions.DefaultTopic);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Zulip notification sent to stream {Stream} topic {Topic}")]
    private partial void LogMessageSent(string stream, string topic);
}
