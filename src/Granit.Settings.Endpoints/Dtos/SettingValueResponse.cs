namespace Granit.Settings.Endpoints.Dtos;

/// <summary>
/// Response for a single setting value.
/// </summary>
/// <param name="Name">Setting name.</param>
/// <param name="Value">Resolved value, or <c>null</c> if not set.</param>
public sealed record SettingValueResponse(string Name, string? Value);
