using FluentAssertions;

using Xunit;

namespace DigitalDynamics.Foundation.Localization.Tests;

public sealed class JsonLocalizationDictionaryBuilderTests
{
    [Fact]
    public void Build_ParsesEmbeddedJsonFiles()
    {
        // Arrange — les fichiers Test/fr.json et Test/en.json sont embarqués dans l'assembly de test
        System.Reflection.Assembly assembly = typeof(JsonLocalizationDictionaryBuilderTests).Assembly;
        string prefix = "DigitalDynamics.Foundation.Localization.Tests.TestResources.Localization.Test";

        // Act
        Dictionary<string, Dictionary<string, string>> result =
            Json.JsonLocalizationDictionaryBuilder.Build(assembly, prefix);

        // Assert
        result.Should().ContainKey("fr");
        result.Should().ContainKey("en");
        result["fr"].Should().ContainKey("Test:Hello");
        result["fr"]["Test:Hello"].Should().Be("Bonjour");
        result["en"]["Test:Hello"].Should().Be("Hello");
    }

    [Fact]
    public void Build_ReturnsEmptyForNonExistentPrefix()
    {
        // Arrange
        System.Reflection.Assembly assembly = typeof(JsonLocalizationDictionaryBuilderTests).Assembly;

        // Act
        Dictionary<string, Dictionary<string, string>> result =
            Json.JsonLocalizationDictionaryBuilder.Build(assembly, "NonExistent.Prefix");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_ParsesParameterizedValues()
    {
        // Arrange
        System.Reflection.Assembly assembly = typeof(JsonLocalizationDictionaryBuilderTests).Assembly;
        string prefix = "DigitalDynamics.Foundation.Localization.Tests.TestResources.Localization.Test";

        // Act
        Dictionary<string, Dictionary<string, string>> result =
            Json.JsonLocalizationDictionaryBuilder.Build(assembly, prefix);

        // Assert
        result["fr"]["Test:Welcome"].Should().Contain("{0}");
        result["fr"]["Test:Welcome"].Should().Contain("{1}");
    }

    [Fact]
    public void Build_ParsesParentResourceFiles()
    {
        // Arrange
        System.Reflection.Assembly assembly = typeof(JsonLocalizationDictionaryBuilderTests).Assembly;
        string prefix = "DigitalDynamics.Foundation.Localization.Tests.TestResources.Localization.Parent";

        // Act
        Dictionary<string, Dictionary<string, string>> result =
            Json.JsonLocalizationDictionaryBuilder.Build(assembly, prefix);

        // Assert
        result.Should().ContainKey("fr");
        result["fr"].Should().ContainKey("Parent:SharedKey");
        result["fr"]["Parent:SharedKey"].Should().Be("Valeur partagée du parent");
    }
}
