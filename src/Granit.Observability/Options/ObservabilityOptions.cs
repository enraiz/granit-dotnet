namespace Granit.Observability.Options;

/// <summary>
/// Configuration options for observability (logs, traces, metrics).
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>Section key in the configuration.</summary>
    public const string SectionName = "Observability";

    /// <summary>Service name for OTEL (e.g. "guava-backend").</summary>
    public string ServiceName { get; set; } = "unknown-service";

    /// <summary>Service version.</summary>
    public string ServiceVersion { get; set; } = "0.0.0";

    /// <summary>OTLP gRPC endpoint (e.g. http://otel-collector:4317).</summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>Service namespace (e.g. "guava-health").</summary>
    public string ServiceNamespace { get; set; } = "guava-health";

    /// <summary>Deployment environment (e.g. "production", "staging", "development").</summary>
    public string Environment { get; set; } = "development";

    /// <summary>Enable trace export via OTLP. Default: true.</summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>Enable metrics export via OTLP. Default: true.</summary>
    public bool EnableMetrics { get; set; } = true;
}
