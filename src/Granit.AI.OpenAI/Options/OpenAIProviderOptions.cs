using Microsoft.Extensions.Options;

namespace Granit.AI.OpenAI.Options;

/// <summary>
/// Configuration options for the OpenAI AI provider.
/// </summary>
/// <remarks>
/// Bound to the <c>AI:OpenAI</c> configuration section.
/// The <see cref="ApiKey"/> should be injected from <c>Granit.Vault</c>; never hardcode it.
/// </remarks>
public sealed class OpenAIProviderOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AI:OpenAI";

    /// <summary>
    /// OpenAI API key. Required. Inject from <c>Granit.Vault</c>; never hardcode.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom endpoint URL for OpenAI-compatible APIs (e.g. Azure OpenAI, local proxies).
    /// </summary>
    /// <remarks>
    /// When <c>null</c> or empty, the default OpenAI endpoint (<c>https://api.openai.com/v1</c>) is used.
    /// </remarks>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Default chat model to use when the workspace does not specify one.
    /// </summary>
    public string DefaultModel { get; set; } = "gpt-4o";

    /// <summary>
    /// Default embedding model to use for embedding generation.
    /// </summary>
    public string DefaultEmbeddingModel { get; set; } = "text-embedding-3-small";
}

/// <summary>
/// Validates <see cref="OpenAIProviderOptions"/> at startup (fail-fast on missing configuration).
/// </summary>
internal sealed class OpenAIProviderOptionsValidator : IValidateOptions<OpenAIProviderOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, OpenAIProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.ApiKey)} must be non-empty. " +
                "Inject it from Granit.Vault; never hardcode API keys.");
        }

        return ValidateOptionsResult.Success;
    }
}
