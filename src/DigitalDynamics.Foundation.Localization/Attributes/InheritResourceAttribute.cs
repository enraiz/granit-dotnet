// ---------------------------------------------------------------------------
// InheritResourceAttribute.cs
// Déclare qu'une ressource de localisation hérite des traductions
// d'une ou plusieurs ressources parentes (héritage ABP-style).
// ---------------------------------------------------------------------------

namespace DigitalDynamics.Foundation.Localization.Attributes;

/// <summary>
/// Déclare que cette ressource hérite des traductions des ressources parentes spécifiées.
/// Les clés non trouvées dans cette ressource seront recherchées dans les parents.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class InheritResourceAttribute(params Type[] baseResourceTypes) : Attribute
{
    /// <summary>
    /// Types des ressources parentes dont hériter les traductions.
    /// </summary>
    public Type[] BaseResourceTypes { get; } = baseResourceTypes;
}
