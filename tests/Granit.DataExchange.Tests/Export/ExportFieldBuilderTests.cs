using Granit.DataExchange.Export;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Export;

public sealed class ExportFieldBuilderTests
{
    [Fact]
    public void Header_SetsHeaderValue_AndReturnsSelf()
    {
        ExportFieldBuilder builder = new();

        ExportFieldBuilder result = builder.Header("Patient Name");

        result.ShouldBeSameAs(builder);
        builder.HeaderValue.ShouldBe("Patient Name");
    }

    [Fact]
    public void Format_SetsFormatValue_AndReturnsSelf()
    {
        ExportFieldBuilder builder = new();

        ExportFieldBuilder result = builder.Format("dd/MM/yyyy");

        result.ShouldBeSameAs(builder);
        builder.FormatValue.ShouldBe("dd/MM/yyyy");
    }

    [Fact]
    public void Order_SetsOrderValue_AndReturnsSelf()
    {
        ExportFieldBuilder builder = new();

        ExportFieldBuilder result = builder.Order(5);

        result.ShouldBeSameAs(builder);
        builder.OrderValue.ShouldBe(5);
    }

    [Fact]
    public void DefaultValues_AreNullOrZero()
    {
        ExportFieldBuilder builder = new();

        builder.HeaderValue.ShouldBeNull();
        builder.FormatValue.ShouldBeNull();
        builder.OrderValue.ShouldBe(0);
    }

    [Fact]
    public void FluentChaining_SetsAllValues()
    {
        ExportFieldBuilder builder = new();

        builder.Header("Amount").Format("#,##0.00").Order(3);

        builder.HeaderValue.ShouldBe("Amount");
        builder.FormatValue.ShouldBe("#,##0.00");
        builder.OrderValue.ShouldBe(3);
    }
}
