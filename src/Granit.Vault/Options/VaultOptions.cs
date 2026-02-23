namespace Granit.Vault.Options;

/// <summary>
/// Configuration options for the HashiCorp Vault client.
/// </summary>
public sealed class VaultOptions
{
    /// <summary>Section key in the configuration.</summary>
    public const string SectionName = "Vault";

    /// <summary>Address of the Vault server (e.g. https://vault.guava-health.com).</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Authentication method: "Kubernetes" or "Token" (dev only).</summary>
    public string AuthMethod { get; set; } = "Kubernetes";

    /// <summary>Vault token (local development only — NEVER in production).</summary>
    public string? Token { get; set; }

    /// <summary>Kubernetes role for Kubernetes authentication.</summary>
    public string KubernetesRole { get; set; } = "guava-backend";

    /// <summary>Path to the Kubernetes JWT for authentication. Default: /var/run/secrets/kubernetes.io/serviceaccount/token.</summary>
    public string KubernetesTokenPath { get; set; } = "/var/run/secrets/kubernetes.io/serviceaccount/token";

    /// <summary>Mount point for the Database engine. Default: "database".</summary>
    public string DatabaseMountPoint { get; set; } = "database";

    /// <summary>Database role name for dynamic credentials. Default: "readwrite".</summary>
    public string DatabaseRoleName { get; set; } = "readwrite";

    /// <summary>Mount point for the Transit engine. Default: "transit".</summary>
    public string TransitMountPoint { get; set; } = "transit";

    /// <summary>Lease renewal interval (percentage of TTL). Default: 0.75 (75%).</summary>
    public double LeaseRenewalThreshold { get; set; } = 0.75;
}
