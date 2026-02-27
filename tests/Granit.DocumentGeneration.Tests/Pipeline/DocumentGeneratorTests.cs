using FluentAssertions;
using Granit.DocumentGeneration.Exceptions;
using Granit.DocumentGeneration.Internal;
using Granit.DocumentGeneration.Pipeline;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using NSubstitute;
using Xunit;

namespace Granit.DocumentGeneration.Tests.Pipeline;

// Declared at namespace scope so NSubstitute can proxy
internal sealed record InvoiceData(string CustomerName, decimal Amount);

internal sealed class InvoiceTemplateType : DocumentTemplateType<InvoiceData>
{
    public override string Name => "Billing.Invoice";
}

public sealed class DocumentGeneratorTests
{
    private static readonly InvoiceTemplateType TemplateType = new();

    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46]; // %PDF magic

    [Fact]
    public async Task GenerateAsync_WithMatchingRenderer_ReturnsDocumentResult()
    {
        // Arrange
        InvoiceData data = new("Hôpital Saint-Luc", 1500m);

        ITextTemplateRenderer textRenderer = Substitute.For<ITextTemplateRenderer>();
        textRenderer.RenderAsync(
                Arg.Any<InvoiceTemplateType>(),
                Arg.Any<InvoiceData>(),
                Arg.Any<CancellationToken>())
            .Returns(new RenderedTextResult("<p>Invoice for Hôpital Saint-Luc</p>"));

        IDocumentRenderer pdfRenderer = Substitute.For<IDocumentRenderer>();
        pdfRenderer.CanRender(DocumentFormat.Pdf).Returns(true);
        pdfRenderer.RenderAsync(
                Arg.Any<string>(),
                DocumentFormat.Pdf,
                Arg.Any<CancellationToken>())
            .Returns(new DocumentResult(PdfBytes, DocumentFormat.Pdf));

        DocumentGenerator sut = new(textRenderer, [pdfRenderer]);

        // Act
        DocumentResult result = await sut.GenerateAsync(
            TemplateType, data, ct: TestContext.Current.CancellationToken);

        // Assert
        result.Format.Should().Be(DocumentFormat.Pdf);
        result.Content.ToArray().Should().Equal(PdfBytes);
    }

    [Fact]
    public async Task GenerateAsync_UsesDefaultFormatFromTemplateType()
    {
        // Arrange — no explicit format override; template default is Pdf
        InvoiceData data = new("Clinique", 200m);

        ITextTemplateRenderer textRenderer = Substitute.For<ITextTemplateRenderer>();
        textRenderer.RenderAsync(
                Arg.Any<InvoiceTemplateType>(),
                Arg.Any<InvoiceData>(),
                Arg.Any<CancellationToken>())
            .Returns(new RenderedTextResult("<p>ok</p>"));

        IDocumentRenderer pdfRenderer = Substitute.For<IDocumentRenderer>();
        pdfRenderer.CanRender(DocumentFormat.Pdf).Returns(true);
        pdfRenderer.RenderAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentFormat>(),
                Arg.Any<CancellationToken>())
            .Returns(new DocumentResult(PdfBytes, DocumentFormat.Pdf));

        DocumentGenerator sut = new(textRenderer, [pdfRenderer]);

        // Act — no targetFormat parameter
        DocumentResult result = await sut.GenerateAsync(
            TemplateType, data, ct: TestContext.Current.CancellationToken);

        // Assert — Pdf was passed to the renderer
        await pdfRenderer.Received(1).RenderAsync(
            Arg.Any<string>(),
            DocumentFormat.Pdf,
            Arg.Any<CancellationToken>());

        result.Format.Should().Be(DocumentFormat.Pdf);
    }

    [Fact]
    public async Task GenerateAsync_WithFormatOverride_UsesOverriddenFormat()
    {
        // Arrange
        InvoiceData data = new("Test", 0m);

        ITextTemplateRenderer textRenderer = Substitute.For<ITextTemplateRenderer>();
        textRenderer.RenderAsync(
                Arg.Any<InvoiceTemplateType>(),
                Arg.Any<InvoiceData>(),
                Arg.Any<CancellationToken>())
            .Returns(new RenderedTextResult("<html/>"));

        IDocumentRenderer excelRenderer = Substitute.For<IDocumentRenderer>();
        excelRenderer.CanRender(DocumentFormat.Excel).Returns(true);
        excelRenderer.RenderAsync(
                Arg.Any<string>(),
                DocumentFormat.Excel,
                Arg.Any<CancellationToken>())
            .Returns(new DocumentResult(new byte[] { 0x50, 0x4B }, DocumentFormat.Excel));

        DocumentGenerator sut = new(textRenderer, [excelRenderer]);

        // Act — override default Pdf with Excel
        DocumentResult result = await sut.GenerateAsync(
            TemplateType, data,
            targetFormat: DocumentFormat.Excel,
            ct: TestContext.Current.CancellationToken);

        // Assert
        result.Format.Should().Be(DocumentFormat.Excel);
    }

    [Fact]
    public async Task GenerateAsync_NoMatchingRenderer_ThrowsDocumentRendererNotFoundException()
    {
        // Arrange — renderer only handles Excel, but we request Pdf
        InvoiceData data = new("X", 0m);

        ITextTemplateRenderer textRenderer = Substitute.For<ITextTemplateRenderer>();
        textRenderer.RenderAsync(
                Arg.Any<InvoiceTemplateType>(),
                Arg.Any<InvoiceData>(),
                Arg.Any<CancellationToken>())
            .Returns(new RenderedTextResult("<html/>"));

        IDocumentRenderer excelOnly = Substitute.For<IDocumentRenderer>();
        excelOnly.CanRender(DocumentFormat.Pdf).Returns(false);
        excelOnly.CanRender(DocumentFormat.Excel).Returns(true);

        DocumentGenerator sut = new(textRenderer, [excelOnly]);

        // Act
        Func<Task> act = async () =>
            await sut.GenerateAsync(
                TemplateType, data,
                targetFormat: DocumentFormat.Pdf,
                ct: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DocumentRendererNotFoundException>()
            .Where(e => e.Format == DocumentFormat.Pdf);
    }

    [Fact]
    public async Task GenerateAsync_HtmlPassedToRenderer_IsFromTextPipeline()
    {
        // Arrange — verifies the rendered HTML is forwarded as-is to the document renderer
        InvoiceData data = new("Dupont", 999m);
        string expectedHtml = "<h1>Invoice</h1><p>Total: 999</p>";
        string? capturedHtml = null;

        ITextTemplateRenderer textRenderer = Substitute.For<ITextTemplateRenderer>();
        textRenderer.RenderAsync(
                Arg.Any<InvoiceTemplateType>(),
                Arg.Any<InvoiceData>(),
                Arg.Any<CancellationToken>())
            .Returns(new RenderedTextResult(expectedHtml));

        IDocumentRenderer renderer = Substitute.For<IDocumentRenderer>();
        renderer.CanRender(DocumentFormat.Pdf).Returns(true);
        renderer.RenderAsync(
                Arg.Do<string>(h => capturedHtml = h),
                Arg.Any<DocumentFormat>(),
                Arg.Any<CancellationToken>())
            .Returns(new DocumentResult(PdfBytes, DocumentFormat.Pdf));

        DocumentGenerator sut = new(textRenderer, [renderer]);

        // Act
        await sut.GenerateAsync(
            TemplateType, data, ct: TestContext.Current.CancellationToken);

        // Assert
        capturedHtml.Should().Be(expectedHtml);
    }
}
