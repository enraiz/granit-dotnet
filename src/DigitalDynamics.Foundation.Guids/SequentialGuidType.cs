// =============================================================================
// SequentialGuidType - Type de GUID sequentiel selon la base de donnees
// =============================================================================
// Chaque moteur de base de donnees trie les GUID differemment dans ses index.
// Le type sequentiel doit correspondre au moteur utilise pour garantir
// l'ordonnancement correct dans l'index clustered.
//
// Digital Dynamics utilise PostgreSQL : le defaut est SequentialAsString.
//
// Inspire de Volo.Abp.Guids.SequentialGuidType.
// =============================================================================

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Decrit le type de GUID sequentiel selon le moteur de base de donnees.
/// </summary>
public enum SequentialGuidType
{
    /// <summary>
    /// Le GUID est sequentiel dans sa representation string (<see cref="Guid.ToString()"/>).
    /// Utilise par PostgreSQL et MySQL.
    /// </summary>
    SequentialAsString,

    /// <summary>
    /// Le GUID est sequentiel dans sa representation binaire (<see cref="Guid.ToByteArray()"/>).
    /// Utilise par Oracle.
    /// </summary>
    SequentialAsBinary,

    /// <summary>
    /// La portion sequentielle est placee a la fin du bloc Data4.
    /// Utilise par SQL Server.
    /// </summary>
    SequentialAtEnd
}
