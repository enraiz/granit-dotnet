namespace Granit.Vault.Aws.Services;

/// <summary>
/// Provides dynamic database credentials from AWS Secrets Manager.
/// Mirrors <c>IDatabaseCredentialProvider</c> from Granit.Vault.
/// </summary>
public interface IAwsDatabaseCredentialProvider
{
    /// <summary>Current database username.</summary>
    string Username { get; }

    /// <summary>Current database password.</summary>
    string Password { get; }

    /// <summary>Indicates whether credentials are available.</summary>
    bool IsReady { get; }
}
