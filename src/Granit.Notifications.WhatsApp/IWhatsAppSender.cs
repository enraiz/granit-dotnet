namespace Granit.Notifications.WhatsApp;

/// <summary>
/// Abstraction for sending WhatsApp messages via Business API.
/// Uses pre-approved Meta templates (not free text like SMS).
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>Sends a WhatsApp message.</summary>
    Task SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default);
}
