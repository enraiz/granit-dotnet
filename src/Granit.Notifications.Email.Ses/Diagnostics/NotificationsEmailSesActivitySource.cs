using System.Diagnostics;

namespace Granit.Notifications.Email.Ses.Diagnostics;

/// <summary>OpenTelemetry activity source for the SES email provider.</summary>
internal static class NotificationsEmailSesActivitySource
{
    /// <summary>Activity source name.</summary>
    public const string Name = "Granit.Notifications.Email.Ses";

    /// <summary>Shared activity source instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    /// <summary>Operation names.</summary>
    internal static class Operations
    {
        public const string SendEmail = "ses.send-email";
    }

    /// <summary>Tag names.</summary>
    internal static class Tags
    {
        public const string Region = "ses.region";
    }
}
