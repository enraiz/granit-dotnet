// ---------------------------------------------------------------------------
// LocalizationResourceNameAttribute.cs
// Associates a short name with a localization resource marker class.
// Used by the system to identify the resource in logs and during debugging.
// ---------------------------------------------------------------------------

namespace DigitalDynamics.Foundation.Localization.Attributes;

/// <summary>
/// Associates a short name with a localization resource marker class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class LocalizationResourceNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Short name of the resource (e.g. "Foundation", "Vault").
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Default culture of the resource, used as the final fallback.
    /// Used by auto-discovery when no explicit registration is present.
    /// </summary>
    public string DefaultCulture { get; init; } = "en";
}
