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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigitalDynamics.Foundation.Vault.Options;
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
public sealed class VaultCredentialLeaseManager : BackgroundService, IDatabaseCredentialProvider
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
            var renewalDelay = TimeSpan.FromSeconds(
                _leaseDurationSeconds * _options.LeaseRenewalThreshold);

            _logger.LogDebug(
                "Next lease renewal in {Delay}",
                renewalDelay);

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
        var path = $"{_options.DatabaseMountPoint}/creds/{_options.DatabaseRoleName}";
        _logger.LogInformation("Obtaining dynamic credentials from {Path}", path);

        var secret = await _vaultClient.V1.Secrets.Database.GetCredentialsAsync(
            _options.DatabaseRoleName,
            mountPoint: _options.DatabaseMountPoint);

        _username = secret.Data.Username;
        _password = secret.Data.Password;
        _leaseId = secret.LeaseId;
        _leaseDurationSeconds = secret.LeaseDurationSeconds;

        _logger.LogInformation(
            "Dynamic credentials obtained: user={Username}, lease={LeaseId}, TTL={TTL}s",
            _username,
            _leaseId,
            _leaseDurationSeconds);
    }

    private async Task RenewLeaseAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Renewing lease {LeaseId}", _leaseId);

        var renewed = await _vaultClient.V1.System.RenewLeaseAsync(
            _leaseId,
            _leaseDurationSeconds);

        _leaseDurationSeconds = renewed.LeaseDurationSeconds;

        _logger.LogInformation(
            "Lease {LeaseId} renewed, new TTL={TTL}s",
            _leaseId,
            _leaseDurationSeconds);
    }
}
