using Granit.Core.Exceptions;

namespace Granit.Vault.Exceptions;

/// <summary>
/// Thrown when the Vault client configuration is invalid (wrong auth method, missing token, etc.).
/// Carries a structured <see cref="IHasErrorCode.ErrorCode"/> for localized resolution
/// by <c>GranitExceptionHandler</c>.
/// </summary>
public sealed class VaultConfigurationException : InvalidOperationException, IHasErrorCode
{
    /// <inheritdoc/>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="VaultConfigurationException"/>.
    /// </summary>
    /// <param name="errorCode">Structured error code (e.g. <c>"Vault:UnknownAuthMethod"</c>).</param>
    /// <param name="message">Technical message for logs and developers.</param>
    public VaultConfigurationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
