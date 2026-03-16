using Granit.AI.OpenAI.Options;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Granit.AI.OpenAI.Tests;

public sealed class OpenAIProviderOptionsValidatorTests
{
    private readonly OpenAIProviderOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidApiKey_Succeeds()
    {
        var options = new OpenAIProviderOptions { ApiKey = "sk-test-key-123" };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyApiKey_Fails()
    {
        var options = new OpenAIProviderOptions { ApiKey = string.Empty };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(OpenAIProviderOptions.ApiKey));
    }

    [Fact]
    public void Validate_NullApiKey_Fails()
    {
        var options = new OpenAIProviderOptions { ApiKey = null! };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(OpenAIProviderOptions.ApiKey));
    }
}
