using FluentAssertions;
using Granit.Templating.Enrichment;
using NSubstitute;
using Xunit;

namespace Granit.Templating.Tests.Enrichment;

// Record declared at namespace scope so NSubstitute can proxy generic interfaces over it
internal sealed record PersonData(string Name, int? Age = null);

public sealed class TemplateDataEnricherPipelineTests
{
    [Fact]
    public async Task EnrichAsync_MultipleEnrichers_ChainedInOrderAndReturnNewInstance()
    {
        // Arrange
        PersonData initial = new("Alice");

        ITemplateDataEnricher<PersonData> addAge = Substitute.For<ITemplateDataEnricher<PersonData>>();
        addAge.Order.Returns(10);
        addAge.EnrichAsync(Arg.Any<PersonData>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<PersonData>() with { Age = 30 }));

        ITemplateDataEnricher<PersonData> upperName = Substitute.For<ITemplateDataEnricher<PersonData>>();
        upperName.Order.Returns(20);
        upperName.EnrichAsync(Arg.Any<PersonData>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                PersonData d = call.Arg<PersonData>();
                return Task.FromResult(d with { Name = d.Name.ToUpperInvariant() });
            });

        // Act — simulate pipeline execution
        IEnumerable<ITemplateDataEnricher<PersonData>> enrichers = [addAge, upperName];
        PersonData current = initial;
        foreach (ITemplateDataEnricher<PersonData> enricher in enrichers.OrderBy(e => e.Order))
        {
            current = await enricher.EnrichAsync(current, TestContext.Current.CancellationToken);
        }

        // Assert
        current.Name.Should().Be("ALICE");
        current.Age.Should().Be(30);
        initial.Age.Should().BeNull(because: "original instance must not be mutated");
        initial.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task EnrichAsync_NoEnrichers_ReturnsOriginalInstance()
    {
        // Arrange
        PersonData original = new("Bob");
        IEnumerable<ITemplateDataEnricher<PersonData>> enrichers = [];

        // Act
        PersonData current = original;
        foreach (ITemplateDataEnricher<PersonData> enricher in enrichers.OrderBy(e => e.Order))
        {
            current = await enricher.EnrichAsync(current, TestContext.Current.CancellationToken);
        }

        // Assert
        current.Should().BeSameAs(original);
    }
}
