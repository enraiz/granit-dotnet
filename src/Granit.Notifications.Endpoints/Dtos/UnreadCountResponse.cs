namespace Granit.Notifications.Endpoints.Dtos;

/// <summary>
/// Response payload for the unread notification count endpoint.
/// </summary>
/// <param name="Count">Number of unread notifications.</param>
public sealed record UnreadCountResponse(int Count);
