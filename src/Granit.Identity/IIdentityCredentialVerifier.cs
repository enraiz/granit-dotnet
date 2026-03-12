namespace Granit.Identity;

/// <summary>
/// Verifies user credentials against the identity provider.
/// Used for re-authentication before sensitive operations.
/// </summary>
public interface IIdentityCredentialVerifier
{
    /// <inheritdoc cref="IIdentityProvider.VerifyUserCredentialsAsync"/>
    Task<bool> VerifyUserCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
