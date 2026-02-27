using System.Reflection;
using FluentAssertions;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Resolvers;
using Xunit;

namespace Granit.Templating.Tests.Resolvers;

public sealed class EmbeddedTemplateResolverTests
{
    private static readonly Assembly TestAssembly = typeof(EmbeddedTemplateResolverTests).Assembly;

    // The test assembly contains:
    //   Granit.Templating.Tests.Templates.Billing.Invoice.html       (neutral)
    //   Granit.Templating.Tests.Templates.Billing.Invoice.fr.html    (culture-specific fr)

    [Fact]
    public async Task TryResolveAsync_WhenResourceDoesNotExist_ReturnsNull()
    {
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        TemplateKey key = new("Test.NonExistent");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public void Priority_IsNegative_SoStoreResolversAlwaysWin()
    {
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        sut.Priority.Should().BeNegative();
    }

    [Fact]
    public async Task TryResolveAsync_NeutralKey_FindsNeutralResource()
    {
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        TemplateKey key = new("Billing.Invoice");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Content.Should().Contain("Invoice neutral");
        result.MimeType.Should().Be("text/html");
        result.RevisionId.Should().BeNull("embedded templates have no persisted revision");
    }

    [Fact]
    public async Task TryResolveAsync_CultureSpecificKey_FindsCultureResource()
    {
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        TemplateKey key = new("Billing.Invoice", "fr");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Content.Should().Contain("Facture fr");
    }

    [Fact]
    public async Task TryResolveAsync_CultureKeyWithNoCultureResource_FallsBackToNeutral()
    {
        // "de" culture has no embedded resource; neutral Billing.Invoice.html exists
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        TemplateKey key = new("Billing.Invoice", "de");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.Should().NotBeNull("must fall back to neutral resource");
        result!.Content.Should().Contain("Invoice neutral");
    }

    [Fact]
    public async Task TryResolveAsync_MultipleAssemblies_SearchesUntilFound()
    {
        // System.Runtime has no Templates resources; TestAssembly has the neutral template
        Assembly emptyAssembly = typeof(object).Assembly;
        EmbeddedTemplateResolver sut = new([emptyAssembly, TestAssembly]);
        TemplateKey key = new("Billing.Invoice");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.Should().NotBeNull("second assembly must be searched when first has no match");
        result!.Content.Should().Contain("Invoice neutral");
    }
}
