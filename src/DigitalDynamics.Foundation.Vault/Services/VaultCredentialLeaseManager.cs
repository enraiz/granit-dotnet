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
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SystemBackend;

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
<<<<<<< HEAD
        _logger.LogInformation("Starting Vault dynamic credential manager");
=======
        LogLeaseManagerStarting(_logger);
>>>>>>> feature/settings-module

        await ObtainCredentialsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan renewalDelay = TimeSpan.FromSeconds(
                _leaseDurationSeconds * _options.LeaseRenewalThreshold);

<<<<<<< HEAD
            _logger.LogDebug(
                "Next lease renewal in {Delay}",
                renewalDelay);
=======
            LogNextRenewal(_logger, renewalDelay);
>>>>>>> feature/settings-module

            await Task.Delay(renewalDelay, stoppingToken);

            try
            {
                await RenewLeaseAsync();
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                _logger.LogWarning(
                    ex,
                    "Lease renewal failed for {LeaseId}, obtaining new credentials",
                    _leaseId);
=======
                LogLeaseRenewalFailed(_logger, _leaseId, ex);
>>>>>>> feature/settings-module

                await ObtainCredentialsAsync();
            }
        }

<<<<<<< HEAD
        _logger.LogInformation("Stopping Vault dynamic credential manager");
=======
        LogLeaseManagerStopping(_logger);
>>>>>>> feature/settings-module
    }

    private async Task ObtainCredentialsAsync()
    {
<<<<<<< HEAD
        var path = $"{_options.DatabaseMountPoint}/creds/{_options.DatabaseRoleName}";
        _logger.LogInformation("Obtaining dynamic credentials from {Path}", path);
=======
        string path = $"{_options.DatabaseMountPoint}/creds/{_options.DatabaseRoleName}";
        LogObtainingCredentials(_logger, path);
>>>>>>> feature/settings-module

        Secret<UsernamePasswordCredentials> secret = await _vaultClient.V1.Secrets.Database.GetCredentialsAsync(
            _options.DatabaseRoleName,
            mountPoint: _options.DatabaseMountPoint);

        _username = secret.Data.Username;
        _password = secret.Data.Password;
        _leaseId = secret.LeaseId;
        _leaseDurationSeconds = secret.LeaseDurationSeconds;

<<<<<<< HEAD
        _logger.LogInformation(
            "Dynamic credentials obtained: user={Username}, lease={LeaseId}, TTL={TTL}s",
            _username,
            _leaseId,
            _leaseDurationSeconds);
=======
        LogCredentialsObtained(_logger, _username, _leaseId, _leaseDurationSeconds);
>>>>>>> feature/settings-module
    }

    private async Task RenewLeaseAsync()
    {
<<<<<<< HEAD
        _logger.LogDebug("Renewing lease {LeaseId}", _leaseId);
=======
        LogLeaseRenewing(_logger, _leaseId);
>>>>>>> feature/settings-module

        Secret<RenewedLease> renewed = await _vaultClient.V1.System.RenewLeaseAsync(
            _leaseId,
            _leaseDurationSeconds);

        _leaseDurationSeconds = renewed.LeaseDurationSeconds;

<<<<<<< HEAD
        _logger.LogInformation(
            "Lease {LeaseId} renewed, new TTL={TTL}s",
            _leaseId,
            _leaseDurationSeconds);
=======
        LogLeaseRenewed(_logger, _leaseId, _leaseDurationSeconds);
>>>>>>> feature/settings-module
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Vault dynamic credentials manager")]
    private static partial void LogLeaseManagerStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping Vault dynamic credentials manager")]
    private static partial void LogLeaseManagerStopping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Next lease renewal in {Delay}")]
    private static partial void LogNextRenewal(ILogger logger, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lease {LeaseId} renewal failed, obtaining new credentials")]
    private static partial void LogLeaseRenewalFailed(ILogger logger, string leaseId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching dynamic credentials from {Path}")]
    private static partial void LogObtainingCredentials(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dynamic credentials obtained: user={Username}, lease={LeaseId}, TTL={TTL}s")]
    private static partial void LogCredentialsObtained(ILogger logger, string username, string leaseId, int ttl);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Renewing lease {LeaseId}")]
    private static partial void LogLeaseRenewing(ILogger logger, string leaseId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Lease {LeaseId} renewed, new TTL={TTL}s")]
    private static partial void LogLeaseRenewed(ILogger logger, string leaseId, int ttl);
}
