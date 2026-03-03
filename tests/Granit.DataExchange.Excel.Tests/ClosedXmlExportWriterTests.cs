using ClosedXML.Excel;
using Granit.DataExchange.Excel.Internal.Export;
using Granit.DataExchange.Export;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Excel.Tests;

public sealed class ClosedXmlExportWriterTests
{
    private static readonly ClosedXmlExportWriter Sut = new();

    // ---- CanWrite ----------------------------------------------------

    [Theory]
    [InlineData("xlsx")]
    [InlineData("XLSX")]
    [InlineData("Xlsx")]
    public void CanWrite_xlsx_returns_true(string format) =>
        Sut.CanWrite(format).ShouldBeTrue();

    [Theory]
    [InlineData("csv")]
    [InlineData("pdf")]
    [InlineData("")]
    public void CanWrite_other_formats_returns_false(string format) =>
        Sut.CanWrite(format).ShouldBeFalse();

    // ---- MimeType / FileExtension ------------------------------------

    [Fact]
    public void MimeType_is_xlsx() =>
        Sut.MimeType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    [Fact]
    public void FileExtension_is_xlsx() =>
        Sut.FileExtension.ShouldBe(".xlsx");

    // ---- WriteAsync --------------------------------------------------

    [Fact]
    public async Task WriteAsync_creates_workbook_with_headers_and_data()
    {
        // Arrange
        List<ExportFieldDescriptor> fields =
        [
            new("Name", "String", "Nom", null, 0, false),
            new("Email", "String", null, null, 1, false),
            new("Age", "Int32", "Âge", null, 2, false),
        ];

        List<IReadOnlyDictionary<string, object?>> rows =
        [
            new Dictionary<string, object?> { ["Name"] = "Alice", ["Email"] = "alice@test.com", ["Age"] = 30 },
            new Dictionary<string, object?> { ["Name"] = "Bob", ["Email"] = "bob@test.com", ["Age"] = 25 },
        ];

        using MemoryStream stream = new();

        // Act
        await Sut.WriteAsync(stream, fields, ToAsyncEnumerable(rows), TestContext.Current.CancellationToken);

        // Assert
        stream.Position = 0;
        using XLWorkbook workbook = new(stream);
        IXLWorksheet ws = workbook.Worksheets.First();

        ws.Cell(1, 1).GetString().ShouldBe("Nom");
        ws.Cell(1, 2).GetString().ShouldBe("Email");
        ws.Cell(1, 3).GetString().ShouldBe("Âge");
        ws.Cell(1, 1).Style.Font.Bold.ShouldBeTrue();

        ws.Cell(2, 1).GetString().ShouldBe("Alice");
        ws.Cell(2, 2).GetString().ShouldBe("alice@test.com");
        ws.Cell(2, 3).GetValue<int>().ShouldBe(30);

        ws.Cell(3, 1).GetString().ShouldBe("Bob");
        ws.Cell(3, 2).GetString().ShouldBe("bob@test.com");
        ws.Cell(3, 3).GetValue<int>().ShouldBe(25);
    }

    [Fact]
    public async Task WriteAsync_empty_data_produces_headers_only()
    {
        // Arrange
        List<ExportFieldDescriptor> fields =
        [
            new("Name", "String", "Nom", null, 0, false),
        ];

        using MemoryStream stream = new();

        // Act
        await Sut.WriteAsync(stream, fields,
            ToAsyncEnumerable([]),
            TestContext.Current.CancellationToken);

        // Assert
        stream.Position = 0;
        using XLWorkbook workbook = new(stream);
        IXLWorksheet ws = workbook.Worksheets.First();

        ws.Cell(1, 1).GetString().ShouldBe("Nom");
        ws.LastRowUsed()!.RowNumber().ShouldBe(1);
    }

    [Fact]
    public async Task WriteAsync_uses_property_path_when_no_header()
    {
        // Arrange
        List<ExportFieldDescriptor> fields =
        [
            new("Company.Name", "String", null, null, 0, true),
        ];

        List<IReadOnlyDictionary<string, object?>> rows =
        [
            new Dictionary<string, object?> { ["Company.Name"] = "Acme" },
        ];

        using MemoryStream stream = new();

        // Act
        await Sut.WriteAsync(stream, fields, ToAsyncEnumerable(rows), TestContext.Current.CancellationToken);

        // Assert
        stream.Position = 0;
        using XLWorkbook workbook = new(stream);
        workbook.Worksheets.First().Cell(1, 1).GetString().ShouldBe("Company.Name");
    }

    [Fact]
    public async Task WriteAsync_handles_null_values()
    {
        // Arrange
        List<ExportFieldDescriptor> fields =
        [
            new("Name", "String", null, null, 0, false),
        ];

        List<IReadOnlyDictionary<string, object?>> rows =
        [
            new Dictionary<string, object?> { ["Name"] = null },
        ];

        using MemoryStream stream = new();

        // Act
        await Sut.WriteAsync(stream, fields, ToAsyncEnumerable(rows), TestContext.Current.CancellationToken);

        // Assert
        stream.Position = 0;
        using XLWorkbook workbook = new(stream);
        workbook.Worksheets.First().Cell(2, 1).GetString().ShouldBeEmpty();
    }

    [Fact]
    public async Task WriteAsync_formats_dates()
    {
        // Arrange
        List<ExportFieldDescriptor> fields =
        [
            new("BirthDate", "DateOnly", null, "yyyy-MM-dd", 0, false),
        ];

        List<IReadOnlyDictionary<string, object?>> rows =
        [
            new Dictionary<string, object?> { ["BirthDate"] = new DateOnly(1990, 6, 15) },
        ];

        using MemoryStream stream = new();

        // Act
        await Sut.WriteAsync(stream, fields, ToAsyncEnumerable(rows), TestContext.Current.CancellationToken);

        // Assert
        stream.Position = 0;
        using XLWorkbook workbook = new(stream);
        IXLCell cell = workbook.Worksheets.First().Cell(2, 1);
        cell.Style.DateFormat.Format.ShouldBe("yyyy-MM-dd");
    }

    // ---- Helpers -----------------------------------------------------

    private static async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ToAsyncEnumerable(
        List<IReadOnlyDictionary<string, object?>> items)
    {
        foreach (IReadOnlyDictionary<string, object?> item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
