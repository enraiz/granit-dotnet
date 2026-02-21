// =============================================================================
// VaultCredentialLeaseManager - Gestion des credentials dynamiques PostgreSQL
// =============================================================================
// BackgroundService qui :
//   1. Obtient un credential dynamique PostgreSQL via Vault Database Engine
//   2. Renouvelle le lease avant expiration (seuil configurable, défaut 75% du TTL)
//   3. Demande un nouveau credential si le renouvellement échoue
//
// Les credentials sont exposés via IDatabaseCredentialProvider pour que
// le DbContext puisse construire sa connection string dynamiquement.
//
// Conformité HDS : aucun mot de passe statique en production.
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
/// Interface pour obtenir les credentials dynamiques PostgreSQL.
/// </summary>
public interface IDatabaseCredentialProvider
{
    /// <summary>Nom d'utilisateur dynamique courant.</summary>
    string Username { get; }

    /// <summary>Mot de passe dynamique courant.</summary>
    string Password { get; }

    /// <summary>Indique si des credentials sont disponibles.</summary>
    bool IsReady { get; }
}

/// <summary>
/// Service d'arrière-plan qui gère le cycle de vie des credentials
/// dynamiques PostgreSQL via Vault Database Engine.
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
        LogLeaseManagerStarting(_logger);

        await ObtainCredentialsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan renewalDelay = TimeSpan.FromSeconds(
                _leaseDurationSeconds * _options.LeaseRenewalThreshold);

            LogNextRenewal(_logger, renewalDelay);

            await Task.Delay(renewalDelay, stoppingToken);

            try
            {
                await RenewLeaseAsync();
            }
            catch (Exception ex)
            {
                LogLeaseRenewalFailed(_logger, _leaseId, ex);

                await ObtainCredentialsAsync();
            }
        }

        LogLeaseManagerStopping(_logger);
    }

    private async Task ObtainCredentialsAsync()
    {
        string path = $"{_options.DatabaseMountPoint}/creds/{_options.DatabaseRoleName}";
        LogObtainingCredentials(_logger, path);

        Secret<UsernamePasswordCredentials> secret = await _vaultClient.V1.Secrets.Database.GetCredentialsAsync(
            _options.DatabaseRoleName,
            mountPoint: _options.DatabaseMountPoint);

        _username = secret.Data.Username;
        _password = secret.Data.Password;
        _leaseId = secret.LeaseId;
        _leaseDurationSeconds = secret.LeaseDurationSeconds;

        LogCredentialsObtained(_logger, _username, _leaseId, _leaseDurationSeconds);
    }

    private async Task RenewLeaseAsync()
    {
        LogLeaseRenewing(_logger, _leaseId);

        Secret<RenewedLease> renewed = await _vaultClient.V1.System.RenewLeaseAsync(
            _leaseId,
            _leaseDurationSeconds);

        _leaseDurationSeconds = renewed.LeaseDurationSeconds;

        LogLeaseRenewed(_logger, _leaseId, _leaseDurationSeconds);
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
