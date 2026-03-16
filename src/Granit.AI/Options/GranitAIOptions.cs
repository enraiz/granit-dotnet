namespace Granit.AI.Options;

/// <summary>
/// Root configuration options for the Granit AI module.
/// </summary>
/// <remarks>
/// Bound to the <c>AI</c> configuration section.
/// </remarks>
public sealed class GranitAIOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AI";

    /// <summary>
    /// Name of the default workspace to use when no workspace is explicitly specified.
    /// </summary>
    public string DefaultWorkspace { get; set; } = "default";

    /// <summary>
    /// Whether to enable usage tracking (token counting and cost estimation).
    /// </summary>
    /// <remarks>
    /// When disabled, <see cref="IAIUsageTracker"/> still receives calls but discards records.
    /// </remarks>
    public bool EnableUsageTracking { get; set; } = true;

    /// <summary>
    /// Whether to enable the audit trail for AI interactions (ISO 27001).
    /// </summary>
    /// <remarks>
    /// When enabled, every <c>IChatClient</c> call is recorded with tenant, user,
    /// workspace, model, token count, and timestamp. Audit entries are immutable.
    /// </remarks>
    public bool EnableAuditTrail { get; set; } = true;
}
