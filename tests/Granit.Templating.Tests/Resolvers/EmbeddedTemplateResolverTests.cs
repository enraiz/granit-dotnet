using System.Reflection;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Resolvers;
using Shouldly;
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

        result.ShouldBeNull();
    }

    [Fact]
    public void Priority_IsNegative_SoStoreResolversAlwaysWin()
    {
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        sut.Priority.ShouldBeLessThan(0);
    }

    [Fact]
    public async Task TryResolveAsync_NeutralKey_FindsNeutralResource()
    {
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        TemplateKey key = new("Billing.Invoice");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Content.ShouldContain("Invoice neutral");
        result.MimeType.ShouldBe("text/html");
        result.RevisionId.ShouldBeNull("embedded templates have no persisted revision");
    }

    [Fact]
    public async Task TryResolveAsync_CultureSpecificKey_FindsCultureResource()
    {
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        TemplateKey key = new("Billing.Invoice", "fr");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Content.ShouldContain("Facture fr");
    }

    [Fact]
    public async Task TryResolveAsync_CultureKeyWithNoCultureResource_FallsBackToNeutral()
    {
        // "de" culture has no embedded resource; neutral Billing.Invoice.html exists
        EmbeddedTemplateResolver sut = new([TestAssembly]);
        TemplateKey key = new("Billing.Invoice", "de");

        TemplateDescriptor? result = await sut.TryResolveAsync(
            key, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull("must fall back to neutral resource");
        result!.Content.ShouldContain("Invoice neutral");
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

        result.ShouldNotBeNull("second assembly must be searched when first has no match");
        result!.Content.ShouldContain("Invoice neutral");
    }
}
