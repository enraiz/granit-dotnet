using FluentAssertions;
using Granit.Templating.Exceptions;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Xunit;

namespace Granit.Templating.Tests.Pipeline;

public sealed class RenderedContentTests
{
    // -------------------------------------------------------------------------
    // BinaryRenderedContent
    // -------------------------------------------------------------------------

    [Fact]
    public void BinaryRenderedContent_ConstructedWithBytesAndFormat_PropertiesMatch()
    {
        byte[] bytes = [0x25, 0x50, 0x44, 0x46]; // %PDF
        ReadOnlyMemory<byte> memory = bytes;

        BinaryRenderedContent result = new(memory, DocumentFormat.Pdf);

        result.Bytes.ToArray().Should().Equal(bytes);
        result.Format.Should().Be(DocumentFormat.Pdf);
    }

    [Fact]
    public void BinaryRenderedContent_RevisionId_CanBeSet()
    {
        Guid id = Guid.NewGuid();
        BinaryRenderedContent result = new(ReadOnlyMemory<byte>.Empty, DocumentFormat.Pdf)
        {
            RevisionId = id,
        };

        result.RevisionId.Should().Be(id);
    }

    // -------------------------------------------------------------------------
    // TextRenderedContent
    // -------------------------------------------------------------------------

    [Fact]
    public void TextRenderedContent_RevisionId_CanBeSet()
    {
        Guid id = Guid.NewGuid();
        TextRenderedContent result = new("<p>ok</p>", DocumentFormat.Html)
        {
            RevisionId = id,
        };

        result.RevisionId.Should().Be(id);
    }

    // -------------------------------------------------------------------------
    // RenderedTextResult
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderedTextResult_WithAllProperties_AllSet()
    {
        RenderedTextResult result = new(
            Html: "<p>Hello</p>",
            PlainText: "Hello",
            Subject: "Test subject");

        result.Html.Should().Be("<p>Hello</p>");
        result.PlainText.Should().Be("Hello");
        result.Subject.Should().Be("Test subject");
    }

    [Fact]
    public void RenderedTextResult_WithOnlyHtml_DefaultsAreNull()
    {
        RenderedTextResult result = new(Html: "<p>Body</p>");

        result.Html.Should().Be("<p>Body</p>");
        result.PlainText.Should().BeNull();
        result.Subject.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // TemplateNotFoundException (culture branch)
    // -------------------------------------------------------------------------

    [Fact]
    public void TemplateNotFoundException_WithCulture_MessageContainsCultureInfo()
    {
        TemplateNotFoundException ex = new("Billing.Invoice", "fr-BE");

        ex.TemplateName.Should().Be("Billing.Invoice");
        ex.Culture.Should().Be("fr-BE");
        ex.Message.Should().Contain("fr-BE");
        ex.Message.Should().Contain("Billing.Invoice");
    }

    [Fact]
    public void TemplateNotFoundException_WithoutCulture_CulturePropertyIsNull()
    {
        TemplateNotFoundException ex = new("Billing.Invoice");

        ex.Culture.Should().BeNull();
        ex.Message.Should().Contain("Billing.Invoice");
    }
}
