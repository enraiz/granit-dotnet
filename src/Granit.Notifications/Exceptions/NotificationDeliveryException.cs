namespace Granit.Notifications.Exceptions;

/// <summary>
/// Exception thrown when a notification delivery fails via a channel.
/// Triggers Wolverine retry policy.
/// </summary>
public sealed class NotificationDeliveryException : Exception
{
    public NotificationDeliveryException(string message) : base(message) { }
    public NotificationDeliveryException(string message, Exception innerException) : base(message, innerException) { }
}
