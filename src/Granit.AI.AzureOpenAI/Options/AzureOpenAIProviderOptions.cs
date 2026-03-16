using Microsoft.Extensions.Options;

namespace Granit.AI.AzureOpenAI.Options;

/// <summary>
/// Configuration for the Azure OpenAI provider.
/// </summary>
/// <remarks>
/// Bound to the <c>AI:AzureOpenAI</c> configuration section.
/// When <see cref="ApiKey"/> is empty, the provider falls back to
/// <c>DefaultAzureCredential</c> (Managed Identity) — the recommended
/// approach for production deployments.
/// </remarks>
public sealed class AzureOpenAIProviderOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AI:AzureOpenAI";

    /// <summary>
    /// Azure OpenAI resource endpoint (e.g. <c>https://my-resource.openai.azure.com</c>).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication. When empty, <c>DefaultAzureCredential</c> is used instead.
    /// </summary>
    /// <remarks>
    /// Inject from <c>Granit.Vault</c>; never hardcode. Leave empty in production
    /// to use Managed Identity (zero secrets).
    /// </remarks>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Default deployment name (e.g. <c>gpt-4o</c>). Used when a workspace does not specify a model.
    /// </summary>
    public string DefaultDeployment { get; set; } = "gpt-4o";

    /// <summary>
    /// Default embedding deployment name.
    /// </summary>
    public string DefaultEmbeddingDeployment { get; set; } = "text-embedding-3-small";
}

/// <summary>
/// Validates <see cref="AzureOpenAIProviderOptions"/> at startup.
/// </summary>
internal sealed class AzureOpenAIProviderOptionsValidator : IValidateOptions<AzureOpenAIProviderOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, AzureOpenAIProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.Endpoint)} must be non-empty. " +
                "Set it to your Azure OpenAI resource endpoint (e.g. https://my-resource.openai.azure.com).");
        }

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.Endpoint)} must be a valid HTTPS URI. Got: '{options.Endpoint}'.");
        }

        return ValidateOptionsResult.Success;
    }
}
