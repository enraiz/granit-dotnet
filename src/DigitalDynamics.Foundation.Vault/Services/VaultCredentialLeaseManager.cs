// =============================================================================
// VaultCredentialLeaseManager - PostgreSQL dynamic credentials management
// =============================================================================
// BackgroundService that:
//   1. Obtains a dynamic PostgreSQL credential via Vault Database Engine
//   2. Renews the lease before expiration (configurable threshold, default 75% of TTL)
//   3. Requests a new credential if renewal fails
//
// Credentials are exposed via IDatabaseCredentialProvider so that
// the DbContext can dynamically build its connection string.
//
// HDS compliance: no static password in production.
// =============================================================================

using DigitalDynamics.Foundation.Vault.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;

namespace DigitalDynamics.Foundation.Vault.Services;

/// <summary>
/// Interface for obtaining dynamic PostgreSQL credentials.
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

/// <summary>
/// Background service that manages the lifecycle of dynamic
/// PostgreSQL credentials via Vault Database Engine.
/// </summary>
public sealed partial class VaultCredentialLeaseManager : BackgroundService, IDatabaseCredentialProvider
{
    private readonly IVaultClient _vaultClient;
    private readonly VaultOptions _options;
    private readonly ILogger<VaultCredentialLeaseManager> _logger;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _leaseId = string.Empty;
    private int _leaseDurationSeconds;

    public VaultCredentialLeaseManager(
        IVaultClient vaultClient,
        IOptions<VaultOptions> options,
        ILogger<VaultCredentialLeaseManager> logger)
    {
        _vaultClient = vaultClient;
        _options = options.Value;
        _logger = logger;
    }

    public string Username => _username;
    public string Password => _password;
    public bool IsReady => !string.IsNullOrEmpty(_username);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Vault dynamic credential manager");

        await ObtainCredentialsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan renewalDelay = TimeSpan.FromSeconds(
                _leaseDurationSeconds * _options.LeaseRenewalThreshold);

            LogNextRenewalIn(_logger, renewalDelay);

            await Task.Delay(renewalDelay, stoppingToken);

            try
            {
                await RenewLeaseAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Lease renewal failed for {LeaseId}, obtaining new credentials",
                    _leaseId);

                await ObtainCredentialsAsync(stoppingToken);
            }
        }

        _logger.LogInformation("Stopping Vault dynamic credential manager");
    }

    private async Task ObtainCredentialsAsync(CancellationToken cancellationToken)
    {
        string path = $"{_options.DatabaseMountPoint}/creds/{_options.DatabaseRoleName}";
        LogObtainingCredentials(_logger, path);

        var secret = await _vaultClient.V1.Secrets.Database.GetCredentialsAsync(
            _options.DatabaseRoleName,
            mountPoint: _options.DatabaseMountPoint);

        _username = secret.Data.Username;
        _password = secret.Data.Password;
        _leaseId = secret.LeaseId;
        _leaseDurationSeconds = secret.LeaseDurationSeconds;

        LogCredentialsObtained(_logger, _username, _leaseId, _leaseDurationSeconds);
    }

    private async Task RenewLeaseAsync(CancellationToken cancellationToken)
    {
        LogRenewingLease(_logger, _leaseId);

        var renewed = await _vaultClient.V1.System.RenewLeaseAsync(
            _leaseId,
            _leaseDurationSeconds);

        _leaseDurationSeconds = renewed.LeaseDurationSeconds;

        LogLeaseRenewed(_logger, _leaseId, _leaseDurationSeconds);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Next lease renewal in {Delay}")]
    private static partial void LogNextRenewalIn(ILogger logger, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Information, Message = "Obtaining dynamic credentials from {Path}")]
    private static partial void LogObtainingCredentials(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dynamic credentials obtained: user={Username}, lease={LeaseId}, TTL={Ttl}s")]
    private static partial void LogCredentialsObtained(ILogger logger, string username, string leaseId, int ttl);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Renewing lease {LeaseId}")]
    private static partial void LogRenewingLease(ILogger logger, string leaseId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Lease {LeaseId} renewed, new TTL={Ttl}s")]
    private static partial void LogLeaseRenewed(ILogger logger, string leaseId, int ttl);
}
