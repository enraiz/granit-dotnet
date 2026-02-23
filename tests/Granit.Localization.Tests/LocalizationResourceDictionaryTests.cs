using FluentAssertions;
using Granit.Localization.Tests.TestResources;
using Xunit;

namespace Granit.Localization.Tests;

public sealed class LocalizationResourceStoreTests
{
    [Fact]
    public void Add_RegistersResource()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();

        // Act
        LocalizationResourceInfo info = dictionary.Add<TestResource>("fr");

        // Assert
        info.Should().NotBeNull();
        info.ResourceType.Should().Be<TestResource>();
        info.DefaultCulture.Should().Be("fr");
    }

    [Fact]
    public void Get_ReturnsRegisteredResource()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();
        dictionary.Add<TestResource>("fr");

        // Act
        LocalizationResourceInfo info = dictionary.Get<TestResource>();

        // Assert
        info.ResourceType.Should().Be<TestResource>();
    }

    [Fact]
    public void Get_ThrowsForUnregisteredResource()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();

        // Act
        Action act = () => dictionary.Get<TestResource>();

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGetValue_ReturnsTrueForRegistered()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();
        dictionary.Add<TestResource>("fr");

        // Act
        bool found = dictionary.TryGetValue(typeof(TestResource), out LocalizationResourceInfo? info);

        // Assert
        found.Should().BeTrue();
        info.Should().NotBeNull();
    }

    [Fact]
    public void TryGetValue_ReturnsFalseForUnregistered()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();

        // Act
        bool found = dictionary.TryGetValue(typeof(TestResource), out LocalizationResourceInfo? info);

        // Assert
        found.Should().BeFalse();
        info.Should().BeNull();
    }

    [Fact]
    public void Add_Duplicate_ReplacesExisting()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();
        dictionary.Add<TestResource>("fr");

        // Act
        _ = dictionary.Add<TestResource>("en");

        // Assert
        dictionary.Get<TestResource>().DefaultCulture.Should().Be("en");
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredResources()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();
        dictionary.Add<TestResource>("fr");
        dictionary.Add<ParentTestResource>("fr");

        // Act
        var all = dictionary.GetAll().ToList();

        // Assert
        all.Should().HaveCount(2);
    }

    [Fact]
    public void AddJson_FluentChaining_AddsSource()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();

        // Act
        LocalizationResourceInfo info = dictionary.Add<TestResource>("fr")
            .AddJson(typeof(TestResource).Assembly, "Some.Prefix");

        // Assert
        info.JsonSources.Should().HaveCount(1);
    }

    [Fact]
    public void AddBaseTypes_FluentChaining_AddsBaseTypes()
    {
        // Arrange
        LocalizationResourceStore dictionary = new();

        // Act
        LocalizationResourceInfo info = dictionary.Add<TestResource>("fr")
            .AddBaseTypes(typeof(ParentTestResource));

        // Assert
        info.BaseTypes.Should().HaveCount(1);
        info.BaseTypes.Should().Contain(typeof(ParentTestResource));
    }
}
