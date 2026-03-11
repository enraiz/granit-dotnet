using Granit.Authentication.ApiKeys.Internal;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Tests;

public sealed class ApiKeyGeneratorTests
{
    private readonly ApiKeyGenerator _sut = new();

    [Theory]
    [InlineData(ApiKeyType.Secret, "live", "gk_live_sk_")]
    [InlineData(ApiKeyType.Publishable, "live", "gk_live_pk_")]
    [InlineData(ApiKeyType.Webhook, "test", "gk_test_wh_")]
    [InlineData(ApiKeyType.Ephemeral, "dev", "gk_dev_ep_")]
    public void Generate_ProducesCorrectPrefix(ApiKeyType type, string env, string expectedPrefix)
    {
        ApiKeyGenerationResult result = _sut.Generate(type, env);

        result.RawSecret.ShouldStartWith(expectedPrefix);
        result.Prefix.ShouldBe(expectedPrefix);
    }

    [Fact]
    public void Generate_RawSecretHasSufficientLength()
    {
        ApiKeyGenerationResult result = _sut.Generate(ApiKeyType.Secret, "live");

        // Prefix (11 chars "gk_live_sk_") + 32 random chars = 43 characters
        result.RawSecret.Length.ShouldBeGreaterThanOrEqualTo(43);
    }

    [Fact]
    public void Generate_HashedKeyIsSha256OfRawSecret()
    {
        ApiKeyGenerationResult result = _sut.Generate(ApiKeyType.Secret, "live");

        string expected = ApiKeyGenerator.ComputeSha256(result.RawSecret);
        result.HashedKey.ShouldBe(expected);
    }

    [Fact]
    public void Generate_LastFourCharsMatchRawSecret()
    {
        ApiKeyGenerationResult result = _sut.Generate(ApiKeyType.Secret, "live");

        result.LastFourChars.ShouldBe(result.RawSecret[^4..]);
    }

    [Fact]
    public void Generate_ProducesUniqueKeys()
    {
        var hashes = new HashSet<string>();

        for (int i = 0; i < 1000; i++)
        {
            ApiKeyGenerationResult result = _sut.Generate(ApiKeyType.Secret, "live");
            hashes.Add(result.HashedKey).ShouldBeTrue($"Duplicate key at iteration {i}");
        }
    }

    [Fact]
    public void Generate_ThrowsOnNullEnvironment() =>
        Should.Throw<ArgumentNullException>(() => _sut.Generate(ApiKeyType.Secret, null!));

    [Fact]
    public void ComputeSha256_IsDeterministic()
    {
        string hash1 = ApiKeyGenerator.ComputeSha256("test-input");
        string hash2 = ApiKeyGenerator.ComputeSha256("test-input");

        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void ComputeSha256_ProducesHexString()
    {
        string hash = ApiKeyGenerator.ComputeSha256("test");

        hash.Length.ShouldBe(64); // SHA-256 = 32 bytes = 64 hex chars
        hash.ShouldMatch("^[0-9a-f]{64}$");
    }
}
