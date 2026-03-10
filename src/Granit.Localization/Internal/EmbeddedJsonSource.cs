// ---------------------------------------------------------------------------
// EmbeddedJsonSource.cs
// Represents a source of JSON files embedded in an assembly.
// The ResourcePrefix corresponds to the prefix of embedded resource names
// (e.g. "Granit.Localization.Localization.Granit").
// ---------------------------------------------------------------------------

using System.Reflection;

namespace Granit.Localization.Internal;

/// <summary>
/// Source of localization JSON files embedded in an assembly.
/// </summary>
/// <param name="Assembly">Assembly containing the embedded resources.</param>
/// <param name="ResourcePrefix">Prefix of embedded resource names (dot separator).</param>
internal sealed record EmbeddedJsonSource(Assembly Assembly, string ResourcePrefix);
