using Granit.DataExchange.Import.Parsing;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Parsing;

public sealed class FileParsingOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        FileParsingOptions options = new();

        options.Encoding.ShouldBeNull();
        options.Separator.ShouldBe(",");
        options.QuoteChar.ShouldBe("\"");
        options.SkipRows.ShouldBe(0);
        options.HeaderRowIndex.ShouldBe(0);
        options.SheetName.ShouldBeNull();
        options.DateFormat.ShouldBeNull();
        options.MimeType.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeInitialized()
    {
        FileParsingOptions options = new()
        {
            Encoding = "windows-1252",
            Separator = ";",
            QuoteChar = "'",
            SkipRows = 2,
            HeaderRowIndex = 1,
            SheetName = "Data",
            DateFormat = "dd/MM/yyyy",
            MimeType = "text/csv",
        };

        options.Encoding.ShouldBe("windows-1252");
        options.Separator.ShouldBe(";");
        options.QuoteChar.ShouldBe("'");
        options.SkipRows.ShouldBe(2);
        options.HeaderRowIndex.ShouldBe(1);
        options.SheetName.ShouldBe("Data");
        options.DateFormat.ShouldBe("dd/MM/yyyy");
        options.MimeType.ShouldBe("text/csv");
    }
}
