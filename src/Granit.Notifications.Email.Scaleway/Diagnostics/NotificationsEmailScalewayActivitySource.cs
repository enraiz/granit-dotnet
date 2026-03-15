using System.Diagnostics;

namespace Granit.Notifications.Email.Scaleway.Diagnostics;

/// <summary>OpenTelemetry activity source for the Scaleway email provider.</summary>
internal static class NotificationsEmailScalewayActivitySource
{
    /// <summary>Activity source name.</summary>
    public const string Name = "Granit.Notifications.Email.Scaleway";

    /// <summary>Shared activity source instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    /// <summary>Operation names.</summary>
    internal static class Operations
    {
        public const string Send = "scaleway-email.send";
    }
}
