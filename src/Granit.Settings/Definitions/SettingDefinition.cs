namespace Granit.Settings.Definitions;

/// <summary>
/// Describes a system setting (static metadata).
/// </summary>
public sealed class SettingDefinition
{
    /// <summary>Unique setting name (lookup key).</summary>
    public string Name { get; }

    /// <summary>Default value returned when no provider supplies a value.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Indicates whether the value must be encrypted at rest (via IStringEncryptionService).
    /// The cache stores plain text — encryption applies only at the store layer.
    /// </summary>
    public bool IsEncrypted { get; init; }

    /// <summary>When true, the value can be exposed to clients (public API).</summary>
    public bool IsVisibleToClients { get; init; }

    /// <summary>
    /// When true (default), a lower-priority provider inherits the value from a higher-priority one
    /// when its own value is null. E.g. Tenant inherits Global when IsInherited = true.
    /// </summary>
    public bool IsInherited { get; init; } = true;

    /// <summary>
    /// Allow-list of provider names authorized to store this setting.
    /// Empty list = all providers are authorized.
    /// </summary>
    public IList<string> Providers { get; } = [];

    /// <summary>Display label (UI).</summary>
    public string? DisplayName { get; init; }

    /// <summary>Long description of the setting (UI, documentation).</summary>
    public string? Description { get; init; }

    public SettingDefinition(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
