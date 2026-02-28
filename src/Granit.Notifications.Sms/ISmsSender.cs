namespace Granit.Notifications.Sms;

/// <summary>
/// Abstraction for sending SMS messages. Implemented by providers (Brevo, Twilio, etc.)
/// and registered as Keyed Services.
/// </summary>
public interface ISmsSender
{
    /// <summary>Sends an SMS message.</summary>
    Task SendAsync(SmsMessage message, CancellationToken ct = default);
}
