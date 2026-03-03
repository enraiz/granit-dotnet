namespace Granit.Querying.Meta;

/// <summary>
/// Group-by field metadata for frontend auto-configuration.
/// </summary>
/// <param name="Name">Property name.</param>
/// <param name="Type">CLR type name.</param>
public sealed record GroupByFieldDto(
    string Name,
    string Type);
