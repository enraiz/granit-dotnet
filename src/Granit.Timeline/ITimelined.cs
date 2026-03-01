namespace Granit.Timeline;

/// <summary>
/// Marker interface for entities that participate in the unified activity stream.
/// Entities implementing this interface can have comments, notes, system logs,
/// followers, and attachments associated with them.
/// </summary>
/// <remarks>
/// Pure opt-in marker — no members required. The entity type name is derived
/// from the CLR type name by convention, or can be overridden via
/// <see cref="TimelinedAttribute"/>.
/// </remarks>
public interface ITimelined;
