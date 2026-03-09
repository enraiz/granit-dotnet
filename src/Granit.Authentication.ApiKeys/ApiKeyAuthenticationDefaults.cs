namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Default values for the API key authentication scheme.
/// </summary>
public static class ApiKeyAuthenticationDefaults
{
    /// <summary>
    /// The default authentication scheme name.
    /// </summary>
    public const string AuthenticationScheme = "ApiKey";

    /// <summary>
    /// The prefix that identifies a Granit API key in the Authorization header.
    /// </summary>
    public const string KeyPrefix = "gk_";
}
