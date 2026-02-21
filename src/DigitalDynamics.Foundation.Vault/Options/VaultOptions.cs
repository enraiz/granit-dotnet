// =============================================================================
// VaultOptions - Configuration du client Vault
// =============================================================================
// Bind depuis la section "Vault" de la configuration (IOptions<T> pattern).
//
// Exemple appsettings.json :
//   "Vault": {
//     "Address": "https://vault.guava-health.com",
//     "AuthMethod": "Kubernetes",
//     "KubernetesRole": "guava-backend",
//     "DatabaseMountPoint": "database",
//     "DatabaseRoleName": "readwrite",
//     "TransitMountPoint": "transit"
//   }
// =============================================================================

namespace DigitalDynamics.Foundation.Vault.Options;

/// <summary>
/// Options de configuration pour le client HashiCorp Vault.
/// </summary>
public sealed class VaultOptions
{
    /// <summary>Clé de section dans la configuration.</summary>
    public const string SectionName = "Vault";

    /// <summary>Adresse du serveur Vault (ex: https://vault.guava-health.com).</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Méthode d'authentification : "Kubernetes" ou "Token" (dev uniquement).</summary>
    public string AuthMethod { get; set; } = "Kubernetes";

    /// <summary>Token Vault (développement local uniquement — JAMAIS en production).</summary>
    public string? Token { get; set; }

    /// <summary>Rôle K8s pour l'authentification Kubernetes.</summary>
    public string KubernetesRole { get; set; } = "guava-backend";

    /// <summary>Chemin du JWT K8s pour l'authentification. Défaut : /var/run/secrets/kubernetes.io/serviceaccount/token.</summary>
    public string KubernetesTokenPath { get; set; } = "/var/run/secrets/kubernetes.io/serviceaccount/token";

    /// <summary>Mount point de l'engine Database. Défaut : "database".</summary>
    public string DatabaseMountPoint { get; set; } = "database";

    /// <summary>Nom du rôle Database pour les credentials dynamiques. Défaut : "readwrite".</summary>
    public string DatabaseRoleName { get; set; } = "readwrite";

    /// <summary>Mount point de l'engine Transit. Défaut : "transit".</summary>
    public string TransitMountPoint { get; set; } = "transit";

    /// <summary>Intervalle de renouvellement du lease (pourcentage du TTL). Défaut : 0.75 (75%).</summary>
    public double LeaseRenewalThreshold { get; set; } = 0.75;
}
