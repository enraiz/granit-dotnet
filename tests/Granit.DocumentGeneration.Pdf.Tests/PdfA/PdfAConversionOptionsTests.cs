using Granit.DocumentGeneration.Pdf.PdfA;
using Shouldly;
using Xunit;

namespace Granit.DocumentGeneration.Pdf.Tests.PdfA;

public sealed class PdfAConversionOptionsTests
{
    [Fact]
    public void Defaults_ConformanceLevel_IsPdfA3b()
    {
        PdfAConversionOptions options = new();
        options.ConformanceLevel.ShouldBe(PdfAConformanceLevel.PdfA3b);
    }

    [Fact]
    public void Defaults_FacturXXmlContent_IsNull()
    {
        PdfAConversionOptions options = new();
        options.FacturXXmlContent.ShouldBeNull();
    }

    [Fact]
    public void Defaults_FacturXConformanceLevel_IsNull()
    {
        PdfAConversionOptions options = new();
        options.FacturXConformanceLevel.ShouldBeNull();
    }

    [Fact]
    public void Defaults_DocumentTitle_IsNull()
    {
        PdfAConversionOptions options = new();
        options.DocumentTitle.ShouldBeNull();
    }

    [Fact]
    public void Defaults_DocumentAuthor_IsNull()
    {
        PdfAConversionOptions options = new();
        options.DocumentAuthor.ShouldBeNull();
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
            DocumentAuthor = "Test Corp",
        };

        options.FacturXXmlContent.ShouldBe("<xml>invoice</xml>");
        options.FacturXConformanceLevel.ShouldBe("EN 16931");
        options.DocumentTitle.ShouldBe("Invoice 2026-001");
        options.DocumentAuthor.ShouldBe("Test Corp");
    }

    [Fact]
    public void PdfAConformanceLevel_HasExpectedValues()
    {
        ((int)PdfAConformanceLevel.PdfA3b).ShouldBe(0);
        ((int)PdfAConformanceLevel.PdfA2a).ShouldBe(1);
    }
}
