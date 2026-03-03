using Granit.DataExchange.Import.Internal;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Mapping;

public sealed class LevenshteinTests
{
    [Theory]
    [InlineData("", "", 1.0)]
    [InlineData("abc", "abc", 1.0)]
    [InlineData("ABC", "ABC", 1.0)]
    public void Identical_strings_return_1(string source, string target, double expected)
    {
        double result = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity(source, target);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("abc", "", 0.0)]
    [InlineData("", "abc", 0.0)]
    public void Completely_different_lengths_return_low_score(string source, string target, double expected)
    {
        double result = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity(source, target);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Similar_strings_return_high_score()
    {
        // "PRENOM" vs "PRÉNOM" — 1 char difference out of 6
        double result = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("PRENOM", "PRÉNOM");
        result.ShouldBeGreaterThan(0.8);
    }

    [Fact]
    public void Very_different_strings_return_low_score()
    {
        double result = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("EMAIL", "TELEPHONE");
        result.ShouldBeLessThan(0.5);
    }

    [Fact]
    public void Score_is_symmetric()
    {
        double ab = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("HELLO", "HALLO");
        double ba = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("HALLO", "HELLO");
        ab.ShouldBe(ba);
    }
}
