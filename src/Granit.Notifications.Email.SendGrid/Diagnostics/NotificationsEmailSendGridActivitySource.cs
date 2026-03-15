using System.Diagnostics;

namespace Granit.Notifications.Email.SendGrid.Diagnostics;

/// <summary>OpenTelemetry activity source for the SendGrid email provider.</summary>
internal static class NotificationsEmailSendGridActivitySource
{
    /// <summary>Activity source name.</summary>
    public const string Name = "Granit.Notifications.Email.SendGrid";

    /// <summary>Shared activity source instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    /// <summary>Operation names.</summary>
    internal static class Operations
    {
        public const string Send = "sendgrid.send";
    }
}
