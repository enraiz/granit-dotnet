using Granit.AI.AzureOpenAI.Internal;
using Granit.AI.AzureOpenAI.Options;
using Granit.AI.Workspaces;
using Microsoft.Extensions.AI;
using Shouldly;

namespace Granit.AI.AzureOpenAI.Tests;

public sealed class AzureOpenAIProviderFactoryTests
{
    private static readonly Microsoft.Extensions.Options.IOptions<AzureOpenAIProviderOptions> DefaultOptions =
        Microsoft.Extensions.Options.Options.Create(new AzureOpenAIProviderOptions
        {
            Endpoint = "https://my-resource.openai.azure.com",
            ApiKey = "test-key",
            DefaultDeployment = "gpt-4o",
        });

    private static AIWorkspace CreateWorkspace(string? model = "gpt-4o") =>
        new()
        {
            Name = "test",
            Provider = "AzureOpenAI",
            Model = model!,
        };

    [Fact]
    public void ProviderName_IsAzureOpenAI()
    {
        var factory = new AzureOpenAIProviderFactory(DefaultOptions);

        factory.ProviderName.ShouldBe("AzureOpenAI");
    }

    [Fact]
    public void CreateChatClient_WithApiKey_ReturnsClient()
    {
        var factory = new AzureOpenAIProviderFactory(DefaultOptions);

        IChatClient client = factory.CreateChatClient(CreateWorkspace());

        client.ShouldNotBeNull();
    }

    [Fact]
    public void CreateEmbeddingGenerator_ReturnsGenerator()
    {
        var factory = new AzureOpenAIProviderFactory(DefaultOptions);

        IEmbeddingGenerator<string, Embedding<float>>? generator =
            factory.CreateEmbeddingGenerator(CreateWorkspace());

        generator.ShouldNotBeNull();
    }
}
