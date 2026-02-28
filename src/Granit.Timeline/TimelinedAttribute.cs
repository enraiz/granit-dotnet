namespace Granit.Timeline;

/// <summary>
/// Optional attribute to specify a custom entity type name for the timeline system.
/// If not applied, the CLR type name is used by default.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TimelinedAttribute(string entityTypeName) : Attribute
{
    /// <summary>The entity type name used in polymorphic references.</summary>
    public string EntityTypeName { get; } = entityTypeName;
}
