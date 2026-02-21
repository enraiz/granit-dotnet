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

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Options de configuration pour le module Guids.
/// </summary>
public sealed class GuidGeneratorOptions
{
    /// <summary>
    /// Type de GUID sequentiel par defaut.
    /// <c>null</c> = utilise <see cref="SequentialGuidType.SequentialAsString"/> (PostgreSQL).
    /// </summary>
    public SequentialGuidType? DefaultSequentialGuidType { get; set; }

    /// <summary>
    /// Retourne le type sequentiel configure ou <see cref="SequentialGuidType.SequentialAsString"/>
    /// par defaut (optimise pour PostgreSQL).
    /// </summary>
    public SequentialGuidType GetDefaultSequentialGuidType() => DefaultSequentialGuidType ?? SequentialGuidType.SequentialAsString;
}
