// ---------------------------------------------------------------------------
// EmbeddedJsonSource.cs
// Représente une source de fichiers JSON embarqués dans une assembly.
// Le ResourcePrefix correspond au préfixe des noms de ressources embarquées
// (ex: "DigitalDynamics.Foundation.Localization.Localization.Foundation").
// ---------------------------------------------------------------------------

using System.Reflection;

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Source de fichiers JSON de localisation embarqués dans une assembly.
/// </summary>
/// <param name="Assembly">Assembly contenant les ressources embarquées.</param>
/// <param name="ResourcePrefix">Préfixe des noms de ressources embarquées (séparateur point).</param>
internal sealed record EmbeddedJsonSource(Assembly Assembly, string ResourcePrefix);
