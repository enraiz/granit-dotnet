using Granit.Features.ValueTypes;

namespace Granit.Features.Definitions;

/// <summary>
/// Describes a feature's static metadata: its name, default value, value type, and constraints.
/// </summary>
/// <remarks>
/// Feature definitions are declared in code via <see cref="FeatureDefinitionProvider"/>
/// and aggregated at startup by <see cref="IFeatureDefinitionStore"/>.
/// They are immutable after application startup.
/// </remarks>
public sealed class FeatureDefinition
{
    /// <summary>Unique feature name (lookup key). Convention: <c>"Module.FeatureName"</c>.</summary>
    public string Name { get; }

    /// <summary>
    /// Default value returned when no provider (Tenant, Plan) supplies a value.
    /// Stored as a string regardless of <see cref="ValueType"/>.
    /// </summary>
    public string DefaultValue { get; }

    /// <summary>The storage and validation type of this feature.</summary>
    public FeatureValueType ValueType { get; }

    /// <summary>
    /// Min/max bounds for <see cref="FeatureValueType.Numeric"/> features.
    /// <c>null</c> for other types.
    /// </summary>
    public NumericConstraint? NumericConstraint { get; init; }

    /// <summary>
    /// Allowed values for <see cref="FeatureValueType.Selection"/> features.
    /// <c>null</c> for other types.
    /// </summary>
    public SelectionValues? SelectionValues { get; init; }

    /// <summary>Display label (for admin UI).</summary>
    public string? DisplayName { get; init; }

    /// <summary>Long description (for admin UI and documentation).</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Initializes a new <see cref="FeatureDefinition"/>.
    /// </summary>
    /// <param name="name">Unique feature name.</param>
    /// <param name="defaultValue">Default string value.</param>
    /// <param name="valueType">Storage and validation type.</param>
    public FeatureDefinition(string name, string defaultValue, FeatureValueType valueType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultValue);
        Name = name;
        DefaultValue = defaultValue;
        ValueType = valueType;
    }
}
