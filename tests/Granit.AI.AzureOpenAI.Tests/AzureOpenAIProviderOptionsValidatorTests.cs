using Granit.AI.AzureOpenAI.Options;
using Shouldly;

namespace Granit.AI.AzureOpenAI.Tests;

public sealed class AzureOpenAIProviderOptionsValidatorTests
{
    private readonly AzureOpenAIProviderOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidEndpoint_Succeeds()
    {
        var options = new AzureOpenAIProviderOptions
        {
            Endpoint = "https://my-resource.openai.azure.com",
        };

        _validator.Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyEndpoint_Fails()
    {
        var options = new AzureOpenAIProviderOptions { Endpoint = "" };

        _validator.Validate(null, options).Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void Validate_HttpEndpoint_Fails()
    {
        var options = new AzureOpenAIProviderOptions
        {
            Endpoint = "http://my-resource.openai.azure.com",
        };

        _validator.Validate(null, options).Succeeded.ShouldBeFalse();
    }
}
