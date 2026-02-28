namespace Granit.Notifications;

/// <summary>
/// Polymorphic reference to a business entity for notification linking (Odoo-style chatter).
/// </summary>
/// <param name="EntityType">Logical entity type name (e.g. "Document", "Invoice").</param>
/// <param name="EntityId">Entity identifier as string for polymorphism.</param>
public sealed record EntityReference(string EntityType, string EntityId);
