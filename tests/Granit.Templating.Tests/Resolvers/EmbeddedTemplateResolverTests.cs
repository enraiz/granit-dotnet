using FluentAssertions;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Resolvers;
using Xunit;

namespace Granit.Templating.Tests.Resolvers;

public sealed class EmbeddedTemplateResolverTests
{
    // The test assembly itself has no embedded Templates/* resources,
    // so all lookups return null — this validates the no-match path.

    [Fact]
    public async Task TryResolveAsync_WhenResourceDoesNotExist_ReturnsNull()
    {
        // Arrange — use this test assembly (has no Templates/ resources)
        EmbeddedTemplateResolver sut = new([typeof(EmbeddedTemplateResolverTests).Assembly]);
        TemplateKey key = new("Test.NonExistent");

        // Act
        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Priority_IsNegative_SoStoreResolversAlwaysWin()
    {
        EmbeddedTemplateResolver sut = new([typeof(EmbeddedTemplateResolverTests).Assembly]);
        sut.Priority.Should().BeNegative();
    }
}
