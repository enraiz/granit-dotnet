namespace Granit.Authentication.JwtBearer.BackChannelLogout;

/// <summary>
/// Result of validating an OIDC back-channel <c>logout_token</c>.
/// </summary>
/// <param name="Success">Whether the token is valid.</param>
/// <param name="SessionId">The <c>sid</c> claim (session-specific revocation). May be <c>null</c> if only <c>sub</c> is present.</param>
/// <param name="SubjectId">The <c>sub</c> claim (user-wide revocation). May be <c>null</c> if only <c>sid</c> is present.</param>
/// <param name="Error">Error description when <paramref name="Success"/> is <c>false</c>.</param>
public sealed record BackChannelLogoutResult(
    bool Success,
    string? SessionId,
    string? SubjectId,
    string? Error);
