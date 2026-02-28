namespace Granit.Localization.Endpoints.Dto;

/// <summary>
/// Represents a language available in the application, for use in a language selector UI.
/// </summary>
/// <param name="CultureName">Culture code (e.g. "fr", "en", "fr-CA").</param>
/// <param name="DisplayName">Display name (e.g. "Français", "English").</param>
/// <param name="FlagIcon">Optional flag icon identifier (e.g. "fr", "gb").</param>
/// <param name="IsDefault">Whether this is the default language for the application.</param>
public sealed record LanguageInfoDto(
    string CultureName,
    string DisplayName,
    string? FlagIcon,
    bool IsDefault);
