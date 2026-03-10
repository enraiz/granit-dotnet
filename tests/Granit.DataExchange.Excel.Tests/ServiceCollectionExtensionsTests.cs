using Granit.DataExchange.Excel.Extensions;
using Granit.DataExchange.Export;
using Granit.DataExchange.Import.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Excel.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitDataExchangeExcel_registers_file_parser_as_singleton()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataExchangeExcel();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IFileParser) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitDataExchangeExcel_registers_export_writer_as_singleton()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitDataExchangeExcel();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IExportWriter) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitDataExchangeExcel_returns_service_collection_for_chaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddGranitDataExchangeExcel();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitDataExchangeExcel_file_parser_can_parse_xlsx()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddGranitDataExchangeExcel();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IFileParser parser = provider.GetRequiredService<IFileParser>();

        // Assert
        parser.CanParse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ShouldBeTrue();
    }

    [Fact]
    public void AddGranitDataExchangeExcel_export_writer_can_write_xlsx()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddGranitDataExchangeExcel();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IExportWriter writer = provider.GetRequiredService<IExportWriter>();

        // Assert
        writer.CanWrite("xlsx").ShouldBeTrue();
    }
}
