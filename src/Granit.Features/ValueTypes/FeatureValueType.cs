namespace Granit.Features.ValueTypes;

/// <summary>
/// Defines the storage and validation type of a feature value.
/// </summary>
public enum FeatureValueType
{
    /// <summary>
    /// Boolean feature — stored as <c>"true"</c> or <c>"false"</c>.
    /// Declared via <c>FeatureGroupDefinition.AddToggle()</c>.
    /// </summary>
    Toggle,

    /// <summary>
    /// Integer feature with optional min/max bounds — stored as a base-10 integer string.
    /// Declared via <c>FeatureGroupDefinition.AddNumeric()</c>.
    /// </summary>
    Numeric,

    /// <summary>
    /// Enumerated feature constrained to a fixed set of allowed string values.
    /// Declared via <c>FeatureGroupDefinition.AddSelection()</c>.
    /// </summary>
    Selection,
}
