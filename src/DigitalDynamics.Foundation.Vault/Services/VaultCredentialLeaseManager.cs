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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigitalDynamics.Foundation.Vault.Options;
using VaultSharp;

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
        _logger.LogInformation("Démarrage du gestionnaire de credentials dynamiques Vault");

        await ObtainCredentialsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var renewalDelay = TimeSpan.FromSeconds(
                _leaseDurationSeconds * _options.LeaseRenewalThreshold);

            _logger.LogDebug(
                "Prochain renouvellement du lease dans {Delay}",
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
                    "Échec du renouvellement du lease {LeaseId}, obtention de nouveaux credentials",
                    _leaseId);

                await ObtainCredentialsAsync(stoppingToken);
            }
        }

        _logger.LogInformation("Arrêt du gestionnaire de credentials dynamiques Vault");
    }

    private async Task ObtainCredentialsAsync(CancellationToken cancellationToken)
    {
        var path = $"{_options.DatabaseMountPoint}/creds/{_options.DatabaseRoleName}";
        _logger.LogInformation("Obtention de credentials dynamiques depuis {Path}", path);

        var secret = await _vaultClient.V1.Secrets.Database.GetCredentialsAsync(
            _options.DatabaseRoleName,
            mountPoint: _options.DatabaseMountPoint);

        _username = secret.Data.Username;
        _password = secret.Data.Password;
        _leaseId = secret.LeaseId;
        _leaseDurationSeconds = secret.LeaseDurationSeconds;

        _logger.LogInformation(
            "Credentials dynamiques obtenus : user={Username}, lease={LeaseId}, TTL={TTL}s",
            _username,
            _leaseId,
            _leaseDurationSeconds);
    }

    private async Task RenewLeaseAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Renouvellement du lease {LeaseId}", _leaseId);

        var renewed = await _vaultClient.V1.System.RenewLeaseAsync(
            _leaseId,
            _leaseDurationSeconds);

        _leaseDurationSeconds = renewed.LeaseDurationSeconds;

        _logger.LogInformation(
            "Lease {LeaseId} renouvelé, nouveau TTL={TTL}s",
            _leaseId,
            _leaseDurationSeconds);
    }
}
