using System.Text.Json.Nodes;
using Granit.Querying.SavedViews;
using Shouldly;
using Xunit;

namespace Granit.Querying.Endpoints.Tests;

public sealed class QueryingSchemaExampleProviderTests
{
    private readonly QueryingSchemaExampleProvider _provider = new();

    [Fact]
    public void GetExamples_returns_entry_for_CreateSavedViewRequest()
    {
        IReadOnlyDictionary<Type, JsonNode> examples = _provider.GetExamples();

        examples.ShouldContainKey(typeof(CreateSavedViewRequest));
        JsonNode node = examples[typeof(CreateSavedViewRequest)];
        node.ShouldBeOfType<JsonObject>();
        node["name"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetExamples_returns_entry_for_UpdateSavedViewRequest()
    {
        IReadOnlyDictionary<Type, JsonNode> examples = _provider.GetExamples();

        examples.ShouldContainKey(typeof(UpdateSavedViewRequest));
        JsonNode node = examples[typeof(UpdateSavedViewRequest)];
        node.ShouldBeOfType<JsonObject>();
        node["name"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetExamples_CreateSavedViewRequest_contains_expected_properties()
    {
        IReadOnlyDictionary<Type, JsonNode> examples = _provider.GetExamples();
        var obj = (JsonObject)examples[typeof(CreateSavedViewRequest)];

        obj["name"].ShouldNotBeNull();
        obj["isShared"].ShouldNotBeNull();
        obj["isDefault"].ShouldNotBeNull();
        obj["filterJson"].ShouldNotBeNull();
        obj["sortJson"].ShouldNotBeNull();
        obj["visibleColumnsJson"].ShouldNotBeNull();
    }

    [Fact]
    public void GetExamples_UpdateSavedViewRequest_contains_expected_properties()
    {
        IReadOnlyDictionary<Type, JsonNode> examples = _provider.GetExamples();
        var obj = (JsonObject)examples[typeof(UpdateSavedViewRequest)];

        obj["name"].ShouldNotBeNull();
        obj["isShared"].ShouldNotBeNull();
        obj["filterJson"].ShouldNotBeNull();
        obj["sortJson"].ShouldNotBeNull();
    }

    [Fact]
    public void GetExamples_returns_exactly_two_entries()
    {
        IReadOnlyDictionary<Type, JsonNode> examples = _provider.GetExamples();

        examples.Count.ShouldBe(2);
    }
}
