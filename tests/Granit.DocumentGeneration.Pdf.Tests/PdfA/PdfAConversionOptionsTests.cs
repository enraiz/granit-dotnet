using FluentAssertions;
using Granit.DocumentGeneration.Pdf.PdfA;
using Xunit;

namespace Granit.DocumentGeneration.Pdf.Tests.PdfA;

public sealed class PdfAConversionOptionsTests
{
    [Fact]
    public void Defaults_ConformanceLevel_IsPdfA3b()
    {
        PdfAConversionOptions options = new();
        options.ConformanceLevel.Should().Be(PdfAConformanceLevel.PdfA3b);
    }

    [Fact]
    public void Defaults_FacturXXmlContent_IsNull()
    {
        PdfAConversionOptions options = new();
        options.FacturXXmlContent.Should().BeNull();
    }

    [Fact]
    public void Defaults_FacturXConformanceLevel_IsNull()
    {
        PdfAConversionOptions options = new();
        options.FacturXConformanceLevel.Should().BeNull();
    }

    [Fact]
    public void Defaults_DocumentTitle_IsNull()
    {
        PdfAConversionOptions options = new();
        options.DocumentTitle.Should().BeNull();
    }

    [Fact]
    public void Defaults_DocumentAuthor_IsNull()
    {
        PdfAConversionOptions options = new();
        options.DocumentAuthor.Should().BeNull();
    }

    [Fact]
    public void CanSet_FacturXOptions()
    {
        PdfAConversionOptions options = new()
        {
            ConformanceLevel = PdfAConformanceLevel.PdfA3b,
            FacturXXmlContent = "<xml>invoice</xml>",
            FacturXConformanceLevel = "EN 16931",
            DocumentTitle = "Invoice 2026-001",
            DocumentAuthor = "Guava Health",
        };

        options.FacturXXmlContent.Should().Be("<xml>invoice</xml>");
        options.FacturXConformanceLevel.Should().Be("EN 16931");
        options.DocumentTitle.Should().Be("Invoice 2026-001");
        options.DocumentAuthor.Should().Be("Guava Health");
    }

    [Fact]
    public void PdfAConformanceLevel_HasExpectedValues()
    {
        ((int)PdfAConformanceLevel.PdfA3b).Should().Be(0);
        ((int)PdfAConformanceLevel.PdfA2a).Should().Be(1);
    }
}
