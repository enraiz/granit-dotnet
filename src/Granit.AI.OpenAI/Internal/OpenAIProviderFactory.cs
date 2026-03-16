using System.ClientModel;
using Granit.AI.OpenAI.Options;
using Granit.AI.Workspaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Granit.AI.OpenAI.Internal;

/// <summary>
/// OpenAI implementation of <see cref="IAIProviderFactory"/>.
/// </summary>
/// <remarks>
/// Creates <see cref="IChatClient"/> and <see cref="IEmbeddingGenerator{String, Embedding}"/>
/// instances backed by the OpenAI API via <see cref="OpenAIClient"/>.
/// </remarks>
internal sealed class OpenAIProviderFactory(IOptions<OpenAIProviderOptions> options) : IAIProviderFactory
{
    private readonly OpenAIProviderOptions _options = options.Value;

    /// <inheritdoc/>
    public string ProviderName => "OpenAI";

    /// <inheritdoc/>
    public IChatClient CreateChatClient(AIWorkspace workspace)
    {
        OpenAIClient client = CreateOpenAIClient();
        string model = string.IsNullOrWhiteSpace(workspace.Model) ? _options.DefaultModel : workspace.Model;

        return client.GetChatClient(model).AsIChatClient();
    }

    /// <inheritdoc/>
    public IEmbeddingGenerator<string, Embedding<float>>? CreateEmbeddingGenerator(AIWorkspace workspace)
    {
        OpenAIClient client = CreateOpenAIClient();

        return client.GetEmbeddingClient(_options.DefaultEmbeddingModel).AsIEmbeddingGenerator();
    }

    private OpenAIClient CreateOpenAIClient()
    {
        var credential = new ApiKeyCredential(_options.ApiKey);

        if (!string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(_options.Endpoint) };
            return new OpenAIClient(credential, clientOptions);
        }

        return new OpenAIClient(credential);
    }
}
