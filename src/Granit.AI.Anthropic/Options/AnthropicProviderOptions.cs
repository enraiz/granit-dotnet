using System.ComponentModel.DataAnnotations;

namespace Granit.AI.Anthropic.Options;

/// <summary>
/// Configuration for the Anthropic (Claude) AI provider.
/// </summary>
/// <remarks>
/// Bound to the <c>AI:Anthropic</c> configuration section.
/// The API key is required; the default model is used when a workspace
/// does not specify one explicitly.
/// </remarks>
public sealed class AnthropicProviderOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AI:Anthropic";

    /// <summary>
    /// Anthropic API key. Required.
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default model to use when a workspace does not specify one.
    /// </summary>
    /// <remarks>
    /// Supported models include <c>claude-opus-4-6</c>, <c>claude-sonnet-4-6</c>,
    /// and <c>claude-haiku-4-5</c>.
    /// </remarks>
    public string DefaultModel { get; set; } = "claude-sonnet-4-6";
}
