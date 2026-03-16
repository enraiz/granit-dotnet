using Granit.AI.Ollama.Options;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Granit.AI.Ollama.Tests;

public sealed class OllamaOptionsValidatorTests
{
    private static readonly OllamaOptionsValidator Validator = new();

    private static OllamaOptions ValidOptions() => new()
    {
        Endpoint = "http://localhost:11434",
        DefaultModel = "llama3.2",
    };

    [Fact]
    public void Validate_ValidEndpoint_Succeeds()
    {
        ValidateOptionsResult result = Validator.Validate(null, ValidOptions());

        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Validate_HttpsEndpoint_Succeeds()
    {
        OllamaOptions options = ValidOptions();
        options.Endpoint = "https://ollama.internal:11434";

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("ftp://localhost:11434")]
    [InlineData("tcp://localhost:11434")]
    public void Validate_InvalidEndpoint_Fails(string endpoint)
    {
        OllamaOptions options = ValidOptions();
        options.Endpoint = endpoint;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(OllamaOptions.Endpoint));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyEndpoint_Fails(string endpoint)
    {
        OllamaOptions options = ValidOptions();
        options.Endpoint = endpoint;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(OllamaOptions.Endpoint));
    }
}
