<<<<<<< HEAD
=======
// =============================================================================
// GuidGeneratorOptions - Configuration du module Guids
// =============================================================================
// Configurable via IOptions<GuidGeneratorOptions> dans Program.cs :
//   builder.Services.AddFoundationGuids(options =>
//   {
//       options.DefaultSequentialGuidType = SequentialGuidType.SequentialAtEnd;
//   });
//
// Le défaut est SequentialAsString (PostgreSQL) au lieu de SequentialAtEnd
// (SQL Server).
// =============================================================================

>>>>>>> feature/settings-module
namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Configuration options for the Guids module.
/// </summary>
public sealed class GuidGeneratorOptions
{
    /// <summary>
    /// Default sequential GUID type.
    /// <c>null</c> = uses <see cref="SequentialGuidType.SequentialAsString"/> (PostgreSQL).
    /// </summary>
    public SequentialGuidType? DefaultSequentialGuidType { get; set; }

    /// <summary>
    /// Returns the configured sequential type or <see cref="SequentialGuidType.SequentialAsString"/>
    /// by default (optimized for PostgreSQL).
    /// </summary>
    public SequentialGuidType GetDefaultSequentialGuidType() => DefaultSequentialGuidType ?? SequentialGuidType.SequentialAsString;
}
