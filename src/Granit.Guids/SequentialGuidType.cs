namespace Granit.Guids;

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
