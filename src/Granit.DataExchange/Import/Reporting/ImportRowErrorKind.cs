namespace Granit.DataExchange.Import.Reporting;

/// <summary>
/// Categorizes the type of error encountered on an imported row.
/// </summary>
public enum ImportRowErrorKind
{
    /// <summary>Type conversion failed (string → CLR type).</summary>
    Conversion,

    /// <summary>FluentValidation rule failed.</summary>
    Validation,

    /// <summary>Database persistence error (constraint violation, etc.).</summary>
    Persistence,

    /// <summary>Identity resolution error (ambiguous match, etc.).</summary>
    Identity,
}
