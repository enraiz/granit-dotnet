namespace Granit.Vault;

/// <summary>
/// Provides dynamic database credentials managed by a vault provider.
/// Implemented by provider-specific packages (HashiCorp Vault Database Engine,
/// Azure Key Vault Secrets, AWS Secrets Manager).
/// </summary>
public interface IDatabaseCredentialProvider
{
    /// <summary>Current dynamic username.</summary>
    string Username { get; }

    /// <summary>Current dynamic password.</summary>
    string Password { get; }

    /// <summary>Indicates whether credentials are available.</summary>
    bool IsReady { get; }
}
