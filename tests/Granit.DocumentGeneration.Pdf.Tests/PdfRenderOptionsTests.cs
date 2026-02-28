using FluentAssertions;
using Xunit;

namespace Granit.DocumentGeneration.Pdf.Tests;

public sealed class PdfRenderOptionsTests
{
    [Fact]
    public void SectionName_IsDocumentGenerationPdf() =>
        PdfRenderOptions.SectionName.Should().Be("DocumentGeneration:Pdf");

    [Fact]
    public void Defaults_PaperFormat_IsA4()
    {
        PdfRenderOptions options = new();
        options.PaperFormat.Should().Be("A4");
    }

    [Fact]
    public void Defaults_Landscape_IsFalse()
    {
        PdfRenderOptions options = new();
        options.Landscape.Should().BeFalse();
    }

    [Fact]
    public void Defaults_PrintBackground_IsTrue()
    {
        PdfRenderOptions options = new();
        options.PrintBackground.Should().BeTrue();
    }

    [Fact]
    public void Defaults_Margins_Are10mm()
    {
        PdfRenderOptions options = new();
        options.MarginTop.Should().Be("10mm");
        options.MarginBottom.Should().Be("10mm");
        options.MarginLeft.Should().Be("10mm");
        options.MarginRight.Should().Be("10mm");
    }

    [Fact]
    public void Defaults_MaxConcurrentPages_Is4()
    {
        PdfRenderOptions options = new();
        options.MaxConcurrentPages.Should().Be(4);
    }

    [Fact]
    public void Defaults_HeaderFooter_AreNull()
    {
        PdfRenderOptions options = new();
        options.HeaderTemplate.Should().BeNull();
        options.FooterTemplate.Should().BeNull();
    }

    [Fact]
    public void Defaults_ChromiumExecutablePath_IsNull()
    {
        PdfRenderOptions options = new();
        options.ChromiumExecutablePath.Should().BeNull();
    }
}
