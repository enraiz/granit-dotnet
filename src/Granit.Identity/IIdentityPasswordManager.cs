namespace Granit.Identity;

/// <summary>
/// Password management operations: query last change date, send reset email, set temporary password.
/// </summary>
public interface IIdentityPasswordManager
{
    /// <inheritdoc cref="IIdentityProvider.GetPasswordChangedAtAsync"/>
    Task<DateTimeOffset?> GetPasswordChangedAtAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.SendPasswordResetEmailAsync"/>
    Task SendPasswordResetEmailAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.SetTemporaryPasswordAsync"/>
    Task SetTemporaryPasswordAsync(
        string userId,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
