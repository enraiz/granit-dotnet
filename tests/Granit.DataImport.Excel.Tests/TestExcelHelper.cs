using System.Data;
using Sylvan.Data.Excel;

namespace Granit.DataImport.Excel.Tests;

/// <summary>
/// Helper for generating in-memory <c>.xlsx</c> files using <see cref="ExcelDataWriter"/>.
/// </summary>
internal static class TestExcelHelper
{
    /// <summary>
    /// Creates an in-memory <c>.xlsx</c> stream from a <see cref="DataTable"/>.
    /// The stream is positioned at the beginning after writing.
    /// </summary>
    public static MemoryStream CreateXlsx(DataTable table, string sheetName = "Sheet1")
    {
        MemoryStream stream = new();
        ExcelDataWriterOptions options = new() { OwnsStream = false };

        using (ExcelDataWriter writer = ExcelDataWriter.Create(stream, ExcelWorkbookType.ExcelXml, options))
        {
            using DataTableReader dataReader = new(table);
            writer.Write(dataReader, sheetName);
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a simple <c>.xlsx</c> with columns Name, Email, Age and 5 rows of test data.
    /// </summary>
    public static MemoryStream CreateSimpleXlsx()
    {
        DataTable table = new();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("Age", typeof(string));

        table.Rows.Add("Alice", "alice@example.com", "30");
        table.Rows.Add("Bob", "bob@example.com", "25");
        table.Rows.Add("Charlie", "charlie@example.com", "35");
        table.Rows.Add("Diana", "diana@example.com", "28");
        table.Rows.Add("Eve", "eve@example.com", "32");

        return CreateXlsx(table);
    }

    /// <summary>
    /// Creates a <c>.xlsx</c> with some empty cells (DBNull values).
    /// </summary>
    public static MemoryStream CreateXlsxWithEmptyCells()
    {
        DataTable table = new();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("Phone", typeof(string));

        table.Rows.Add("Alice", "alice@example.com", DBNull.Value);
        table.Rows.Add("Bob", DBNull.Value, "+32123456");

        return CreateXlsx(table);
    }

    /// <summary>
    /// Creates a <c>.xlsx</c> with two worksheets.
    /// </summary>
    public static MemoryStream CreateMultiSheetXlsx()
    {
        MemoryStream stream = new();
        ExcelDataWriterOptions options = new() { OwnsStream = false };

        using (ExcelDataWriter writer = ExcelDataWriter.Create(stream, ExcelWorkbookType.ExcelXml, options))
        {
            // Sheet 1
            DataTable sheet1 = new();
            sheet1.Columns.Add("Id", typeof(string));
            sheet1.Columns.Add("Value", typeof(string));
            sheet1.Rows.Add("1", "First");
            sheet1.Rows.Add("2", "Second");
            using (DataTableReader reader1 = new(sheet1))
            {
                writer.Write(reader1, "Main");
            }

            // Sheet 2
            DataTable sheet2 = new();
            sheet2.Columns.Add("Code", typeof(string));
            sheet2.Columns.Add("Label", typeof(string));
            sheet2.Rows.Add("A", "Alpha");
            sheet2.Rows.Add("B", "Beta");
            sheet2.Rows.Add("C", "Gamma");
            using (DataTableReader reader2 = new(sheet2))
            {
                writer.Write(reader2, "Lookup");
            }
        }

        stream.Position = 0;
        return stream;
    }
}
