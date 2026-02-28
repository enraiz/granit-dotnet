using ClosedXML.Excel;
using FluentAssertions;
using Granit.DocumentGeneration.Excel.Internal;
using Granit.Templating.Pipeline;
using Xunit;

// 'DocumentFormat' is a root namespace from DocumentFormat.OpenXml (transitive ClosedXML dep.).
using TemplatingDocFormat = Granit.Templating.Keys.DocumentFormat;

namespace Granit.DocumentGeneration.Excel.Tests;

public sealed class ClosedXmlTemplateEngineTests
{
    private static readonly string ExcelMimeType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    /// <summary>Creates an in-memory XLSX template with a single sheet containing the given cells.</summary>
    private static string CreateBase64Template(Action<IXLWorksheet> configure)
    {
        using XLWorkbook wb = new();
        IXLWorksheet ws = wb.AddWorksheet("Sheet1");
        configure(ws);
        using MemoryStream ms = new();
        wb.SaveAs(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static ClosedXmlTemplateEngine CreateSut() => new();

    // -------------------------------------------------------------------------
    // CanRender
    // -------------------------------------------------------------------------

    [Fact]
    public void CanRender_WithExcelMimeType_ReturnsTrue()
    {
        ClosedXmlTemplateEngine sut = CreateSut();
        TemplateDescriptor descriptor = new() { Content = string.Empty, MimeType = ExcelMimeType };

        sut.CanRender(descriptor).Should().BeTrue();
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("text/plain")]
    [InlineData("application/pdf")]
    public void CanRender_WithNonExcelMimeType_ReturnsFalse(string mimeType)
    {
        ClosedXmlTemplateEngine sut = CreateSut();
        TemplateDescriptor descriptor = new() { Content = string.Empty, MimeType = mimeType };

        sut.CanRender(descriptor).Should().BeFalse();
    }

    [Fact]
    public void CanRender_CaseInsensitive()
    {
        ClosedXmlTemplateEngine sut = CreateSut();
        TemplateDescriptor descriptor = new()
        {
            Content = string.Empty,
            MimeType = ExcelMimeType.ToUpperInvariant(),
        };

        sut.CanRender(descriptor).Should().BeTrue("MIME type comparison must be case-insensitive");
    }

    // -------------------------------------------------------------------------
    // RenderAsync — basic substitution
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RenderAsync_ReturnsValidXlsxBytes()
    {
        ClosedXmlTemplateEngine sut = CreateSut();
        string base64 = CreateBase64Template(ws => ws.Cell("A1").SetValue("{{model.name}}"));
        TemplateDescriptor descriptor = new() { Content = base64, MimeType = ExcelMimeType };

        RenderedContent result = await sut.RenderAsync(
            descriptor,
            new { Name = "Alice" },
            TemplatingDocFormat.Excel,
            [],
            TestContext.Current.CancellationToken);

        result.Should().BeOfType<BinaryRenderedContent>();
        BinaryRenderedContent binary = (BinaryRenderedContent)result;
        binary.Format.Should().Be(TemplatingDocFormat.Excel);
        binary.Bytes.IsEmpty.Should().BeFalse("must produce non-empty bytes");

        // Verify the output is a valid XLSX (readable by ClosedXML)
        using MemoryStream ms = new(binary.Bytes.ToArray());
        using XLWorkbook wb = new(ms);
        wb.Worksheets.Should().HaveCount(1);
    }

    [Fact]
    public async Task RenderAsync_ReplacesPlaceholders()
    {
        ClosedXmlTemplateEngine sut = CreateSut();
        string base64 = CreateBase64Template(ws =>
        {
            ws.Cell("A1").SetValue("Hello {{model.first_name}} {{model.last_name}}");
        });
        TemplateDescriptor descriptor = new() { Content = base64, MimeType = ExcelMimeType };

        RenderedContent result = await sut.RenderAsync(
            descriptor,
            new { FirstName = "Jean", LastName = "Dupont" },
            TemplatingDocFormat.Excel,
            [],
            TestContext.Current.CancellationToken);

        BinaryRenderedContent binary = (BinaryRenderedContent)result;
        using MemoryStream ms = new(binary.Bytes.ToArray());
        using XLWorkbook wb = new(ms);
        string cellValue = wb.Worksheet(1).Cell("A1").GetValue<string>();
        cellValue.Should().Be("Hello Jean Dupont");
    }

    [Fact]
    public async Task RenderAsync_NestedProperty_ReplacesPlaceholder()
    {
        ClosedXmlTemplateEngine sut = CreateSut();
        string base64 = CreateBase64Template(ws =>
        {
            ws.Cell("A1").SetValue("City: {{model.address.city}}");
        });
        TemplateDescriptor descriptor = new() { Content = base64, MimeType = ExcelMimeType };

        RenderedContent result = await sut.RenderAsync(
            descriptor,
            new { Address = new { City = "Bruxelles" } },
            TemplatingDocFormat.Excel,
            [],
            TestContext.Current.CancellationToken);

        BinaryRenderedContent binary = (BinaryRenderedContent)result;
        using MemoryStream ms = new(binary.Bytes.ToArray());
        using XLWorkbook wb = new(ms);
        string cellValue = wb.Worksheet(1).Cell("A1").GetValue<string>();
        cellValue.Should().Be("City: Bruxelles");
    }

    [Fact]
    public async Task RenderAsync_PropagatesRevisionId()
    {
        ClosedXmlTemplateEngine sut = CreateSut();
        Guid revisionId = Guid.NewGuid();
        string base64 = CreateBase64Template(ws => ws.Cell("A1").SetValue("test"));
        TemplateDescriptor descriptor = new()
        {
            Content = base64,
            MimeType = ExcelMimeType,
            RevisionId = revisionId,
        };

        RenderedContent result = await sut.RenderAsync(
            descriptor,
            new { },
            TemplatingDocFormat.Excel,
            [],
            TestContext.Current.CancellationToken);

        result.RevisionId.Should().Be(revisionId);
    }
}
