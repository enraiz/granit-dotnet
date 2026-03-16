using Microsoft.Extensions.Options;

namespace Granit.AI.Ollama.Options;

/// <summary>
/// Configuration for the Ollama AI provider.
/// </summary>
/// <remarks>
/// <para>
/// Ollama runs AI models locally — no API key is required. By default, the provider
/// connects to <c>http://localhost:11434</c> and uses the <c>llama3.1</c> model.
/// </para>
/// <para>
/// Override <see cref="Endpoint"/> to point to a remote Ollama instance (e.g. on a
/// dedicated GPU server). Override <see cref="DefaultModel"/> to change the fallback
/// model when an <see cref="AIWorkspace"/> does not specify one.
/// </para>
/// </remarks>
public sealed class OllamaOptions
{
    /// <summary>
    /// Configuration section name used for <c>IConfiguration</c> binding.
    /// </summary>
    public const string SectionName = "AI:Ollama";

    /// <summary>
    /// The Ollama server endpoint URL. Defaults to <c>http://localhost:11434</c>.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// The default model to use when an <see cref="AIWorkspace"/> does not specify a model.
    /// Defaults to <c>llama3.1</c>.
    /// </summary>
    public string DefaultModel { get; set; } = "llama3.1";
}

/// <summary>
/// Validates <see cref="OllamaOptions"/> at startup (fail-fast on invalid configuration).
/// </summary>
internal sealed class OllamaOptionsValidator : IValidateOptions<OllamaOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, OllamaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.Endpoint)} must be non-empty. " +
                "Set it to the Ollama server URL (e.g. http://localhost:11434).");
        }

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.Endpoint)} must be a valid HTTP or HTTPS URI. " +
                $"Got: '{options.Endpoint}'.");
        }

        return ValidateOptionsResult.Success;
    }
}
