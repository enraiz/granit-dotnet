using System.Diagnostics;
using Amazon.SimpleNotificationService.Model;
using Granit.Notifications.Sms.Sns.Diagnostics;
using Granit.Notifications.Sms.Sns.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Sms.Sns.Internal;

/// <summary>
/// <see cref="ISmsSender"/> implementation using AWS SNS.
/// Registered as Keyed Service with key "Sns".
/// </summary>
internal sealed partial class SnsSmsSender(
    IOptions<SnsSmsOptions> options,
    ILogger<SnsSmsSender> logger,
    ISnsSmsTransport transport) : ISmsSender
{
    /// <inheritdoc />
    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SnsSmsOptions opts = options.Value;

        using Activity? activity = NotificationsSmsSnsActivitySource.Source.StartActivity(
            NotificationsSmsSnsActivitySource.Operations.SendSms);
        activity?.SetTag(NotificationsSmsSnsActivitySource.Tags.Recipient, message.To);

        Dictionary<string, MessageAttributeValue> attributes = new()
        {
            ["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = opts.SmsType,
            },
        };

        string? senderId = message.SenderId ?? opts.SenderId;
        if (!string.IsNullOrEmpty(senderId))
        {
            attributes["AWS.SNS.SMS.SenderID"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = senderId,
            };
        }

        if (!string.IsNullOrEmpty(opts.OriginationNumber))
        {
            attributes["AWS.MM.SMS.OriginationNumber"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = opts.OriginationNumber,
            };
        }

        PublishRequest request = new()
        {
            Message = message.Body,
            PhoneNumber = message.To,
            MessageAttributes = attributes,
        };

        PublishResponse response = await transport
            .PublishAsync(request, cancellationToken)
            .ConfigureAwait(false);

        LogSmsSent(message.To, response.MessageId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SNS SMS sent to {Recipient}, messageId={MessageId}")]
    private partial void LogSmsSent(string recipient, string messageId);
}
