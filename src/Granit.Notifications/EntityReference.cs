namespace Granit.Notifications;

/// <summary>
/// Polymorphic reference to a business entity (Odoo-style chatter link).
/// </summary>
public sealed record EntityReference(string EntityType, string EntityId);
