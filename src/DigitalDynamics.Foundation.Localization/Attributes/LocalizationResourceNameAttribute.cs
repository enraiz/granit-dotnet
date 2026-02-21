// ---------------------------------------------------------------------------
// LocalizationResourceNameAttribute.cs
// Associe un nom court à une classe marker de ressource de localisation.
// Utilisé par le système pour identifier la ressource dans les logs et le debug.
// ---------------------------------------------------------------------------

namespace DigitalDynamics.Foundation.Localization.Attributes;

/// <summary>
/// Associe un nom court à une classe marker de ressource de localisation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class LocalizationResourceNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Nom court de la ressource (ex: "Foundation", "Vault").
    /// </summary>
    public string Name { get; } = name;
}
