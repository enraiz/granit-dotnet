// =============================================================================
// ObservabilityOptions - Configuration de l'observabilité (Serilog + OTEL)
// =============================================================================
// Bind depuis la section "Observability" de la configuration.
//
// Exemple appsettings.json :
//   "Observability": {
//     "ServiceName": "guava-backend",
//     "ServiceVersion": "1.0.0",
//     "OtlpEndpoint": "http://otel-collector:4317"
//   }
// =============================================================================

namespace DigitalDynamics.Foundation.Observability.Options;

/// <summary>
/// Options de configuration pour l'observabilité (logs, traces, métriques).
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>Clé de section dans la configuration.</summary>
    public const string SectionName = "Observability";

    /// <summary>Nom du service pour OTEL (ex: "guava-backend").</summary>
    public string ServiceName { get; set; } = "unknown-service";

    /// <summary>Version du service.</summary>
    public string ServiceVersion { get; set; } = "0.0.0";

    /// <summary>Endpoint OTLP gRPC (ex: http://otel-collector:4317).</summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>Namespace du service (ex: "guava-health").</summary>
    public string ServiceNamespace { get; set; } = "guava-health";

    /// <summary>Environnement de déploiement (ex: "production", "staging", "development").</summary>
    public string Environment { get; set; } = "development";

    /// <summary>Activer l'export des traces via OTLP. Défaut : true.</summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>Activer l'export des métriques via OTLP. Défaut : true.</summary>
    public bool EnableMetrics { get; set; } = true;
}
