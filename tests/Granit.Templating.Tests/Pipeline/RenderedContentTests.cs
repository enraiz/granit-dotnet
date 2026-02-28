using Granit.Templating.Exceptions;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Shouldly;
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

        result.Bytes.ToArray().ShouldBe(bytes);
        result.Format.ShouldBe(DocumentFormat.Pdf);
    }

    [Fact]
    public void BinaryRenderedContent_RevisionId_CanBeSet()
    {
        Guid id = Guid.NewGuid();
        BinaryRenderedContent result = new(ReadOnlyMemory<byte>.Empty, DocumentFormat.Pdf)
        {
            RevisionId = id,
        };

        result.RevisionId.ShouldBe(id);
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

        result.RevisionId.ShouldBe(id);
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

        result.Html.ShouldBe("<p>Hello</p>");
        result.PlainText.ShouldBe("Hello");
        result.Subject.ShouldBe("Test subject");
    }

    [Fact]
    public void RenderedTextResult_WithOnlyHtml_DefaultsAreNull()
    {
        RenderedTextResult result = new(Html: "<p>Body</p>");

        result.Html.ShouldBe("<p>Body</p>");
        result.PlainText.ShouldBeNull();
        result.Subject.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // TemplateNotFoundException (culture branch)
    // -------------------------------------------------------------------------

    [Fact]
    public void TemplateNotFoundException_WithCulture_MessageContainsCultureInfo()
    {
        TemplateNotFoundException ex = new("Billing.Invoice", "fr-BE");

        ex.TemplateName.ShouldBe("Billing.Invoice");
        ex.Culture.ShouldBe("fr-BE");
        ex.Message.ShouldContain("fr-BE");
        ex.Message.ShouldContain("Billing.Invoice");
    }

    [Fact]
    public void TemplateNotFoundException_WithoutCulture_CulturePropertyIsNull()
    {
        TemplateNotFoundException ex = new("Billing.Invoice");

        ex.Culture.ShouldBeNull();
        ex.Message.ShouldContain("Billing.Invoice");
    }
}
