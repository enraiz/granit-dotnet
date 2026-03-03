using Granit.DataExchange.Import.Internal;
using Granit.DataExchange.Import.Mapping;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Mapping;

public sealed class MappingSuggestionServiceEdgeCaseTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ISemanticMappingService _semanticService = Substitute.For<ISemanticMappingService>();
    private readonly IOptions<ImportOptions> _options;
    private readonly MappingSuggestionService _sut;

    public MappingSuggestionServiceEdgeCaseTests()
    {
        _options = Options.Create(new ImportOptions { FuzzyMatchThreshold = 0.8 });
        _sut = new MappingSuggestionService(_serviceProvider, _semanticService, _options);

        TestPatientImportDefinition definition = new();
        _serviceProvider
            .GetService(typeof(ImportDefinition<TestPatient>))
            .Returns(definition);
    }

    [Fact]
    public async Task Already_matched_header_skipped_in_exact_tier()
    {
        // Arrange — saved mapping takes "Niss", so exact match on "Niss" should not duplicate
        IMappingStore store = Substitute.For<IMappingStore>();
        store.LoadAsync("Test.PatientImport", Arg.Any<CancellationToken>())
            .Returns(new List<ColumnMapping>
            {
                new("Niss", "Niss", MappingConfidence.Saved),
            });
        _serviceProvider.GetService(typeof(IMappingStore)).Returns(store);

        List<string> headers = ["Niss", "FirstName"];

        // Act
        IReadOnlyList<ColumnMapping> result = await _sut.SuggestMappingsAsync<TestPatient>(
            headers, TestContext.Current.CancellationToken);

        // Assert — "Niss" mapped by Saved, "FirstName" by Exact
        result.ShouldContain(m => m.SourceColumn == "Niss" && m.Confidence == MappingConfidence.Saved);
        result.ShouldContain(m => m.SourceColumn == "FirstName" && m.Confidence == MappingConfidence.Exact);
    }

    [Fact]
    public async Task Already_matched_header_skipped_in_fuzzy_tier()
    {
        // Arrange — exact match takes "FirstName", so fuzzy for "Prénom" (which also maps to FirstName) won't duplicate
        List<string> headers = ["FirstName", "Prenom"];

        // Act
        IReadOnlyList<ColumnMapping> result = await _sut.SuggestMappingsAsync<TestPatient>(
            headers, TestContext.Current.CancellationToken);

        // Assert — "FirstName" exact, "Prenom" should NOT also map to FirstName
        result.ShouldContain(m => m.SourceColumn == "FirstName" && m.Confidence == MappingConfidence.Exact);
        result.ShouldNotContain(m => m.SourceColumn == "Prenom" && m.TargetProperty == "FirstName");
    }

    [Fact]
    public async Task Fuzzy_match_via_alias_scores_higher_than_property_name()
    {
        // Arrange — "Mail" is an alias of Email, fuzzy match on "Mails" should match Email via alias
        List<string> headers = ["Mails"];

        // Act
        IReadOnlyList<ColumnMapping> result = await _sut.SuggestMappingsAsync<TestPatient>(
            headers, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain(m => m.SourceColumn == "Mails" &&
            m.TargetProperty == "Email" &&
            m.Confidence == MappingConfidence.Fuzzy);
    }

    [Fact]
    public async Task Saved_mapping_with_null_target_is_skipped()
    {
        // Arrange — saved mapping with null TargetProperty should be filtered out
        IMappingStore store = Substitute.For<IMappingStore>();
        store.LoadAsync("Test.PatientImport", Arg.Any<CancellationToken>())
            .Returns(new List<ColumnMapping>
            {
                new("Unmapped Column", null, MappingConfidence.Saved),
            });
        _serviceProvider.GetService(typeof(IMappingStore)).Returns(store);

        List<string> headers = ["Unmapped Column"];

        // Act
        IReadOnlyList<ColumnMapping> result = await _sut.SuggestMappingsAsync<TestPatient>(
            headers, TestContext.Current.CancellationToken);

        // Assert — null-target saved mapping ignored
        result.ShouldNotContain(m => m.SourceColumn == "Unmapped Column" && m.Confidence == MappingConfidence.Saved);
    }

    [Fact]
    public async Task Saved_mapping_not_in_headers_is_skipped()
    {
        // Arrange — saved mapping for a column not in headers
        IMappingStore store = Substitute.For<IMappingStore>();
        store.LoadAsync("Test.PatientImport", Arg.Any<CancellationToken>())
            .Returns(new List<ColumnMapping>
            {
                new("Ghost Column", "Niss", MappingConfidence.Saved),
            });
        _serviceProvider.GetService(typeof(IMappingStore)).Returns(store);

        List<string> headers = ["FirstName"];

        // Act
        IReadOnlyList<ColumnMapping> result = await _sut.SuggestMappingsAsync<TestPatient>(
            headers, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotContain(m => m.SourceColumn == "Ghost Column");
    }

    [Fact]
    public async Task Semantic_suggestion_for_already_matched_target_is_skipped()
    {
        // Arrange
        _semanticService.IsAvailable.Returns(true);
        _semanticService.SuggestSemanticMappingsAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IReadOnlyList<FieldMetadata>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<SemanticMappingSuggestion>
            {
                new("Custom Column", "Niss", 0.95),
            });

        // "Niss" header will match exactly first, so AI suggestion for target "Niss" should be skipped
        List<string> headers = ["Niss", "Custom Column"];

        // Act
        IReadOnlyList<ColumnMapping> result = await _sut.SuggestMappingsAsync<TestPatient>(
            headers, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain(m => m.SourceColumn == "Niss" && m.Confidence == MappingConfidence.Exact);
        result.ShouldNotContain(m => m.SourceColumn == "Custom Column" && m.Confidence == MappingConfidence.Semantic);
    }

    [Fact]
    public void Levenshtein_empty_strings_returns_1()
    {
        double result = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("", "");
        result.ShouldBe(1.0);
    }

    [Fact]
    public void Levenshtein_one_empty_string_returns_0()
    {
        double result = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("abc", "");
        result.ShouldBe(0.0);
    }

    [Fact]
    public void Levenshtein_single_char_strings()
    {
        double same = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("a", "a");
        double different = MappingSuggestionService.ComputeNormalizedLevenshteinSimilarity("a", "b");

        same.ShouldBe(1.0);
        different.ShouldBe(0.0);
    }
}
