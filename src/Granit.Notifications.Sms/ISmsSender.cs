namespace Granit.Notifications.Sms;

/// <summary>
/// Abstraction for sending SMS messages. Implemented by provider-specific packages
/// and registered as Keyed Services.
/// </summary>
public interface ISmsSender
{
    /// <summary>Sends an SMS message.</summary>
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
