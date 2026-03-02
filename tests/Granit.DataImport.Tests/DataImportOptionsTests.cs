using Shouldly;
using Xunit;

namespace Granit.DataImport.Tests;

public sealed class DataImportOptionsTests
{
    [Fact]
    public void SectionName_is_DataImport()
    {
        DataImportOptions.SectionName.ShouldBe("DataImport");
    }

    [Fact]
    public void Default_max_file_size_is_50()
    {
        DataImportOptions options = new();
        options.DefaultMaxFileSizeMb.ShouldBe(50);
    }

    [Fact]
    public void Default_batch_size_is_500()
    {
        DataImportOptions options = new();
        options.DefaultBatchSize.ShouldBe(500);
    }

    [Fact]
    public void Default_fuzzy_match_threshold_is_0_8()
    {
        DataImportOptions options = new();
        options.FuzzyMatchThreshold.ShouldBe(0.8);
    }

    [Fact]
    public void Properties_are_settable()
    {
        // Arrange & Act
        DataImportOptions options = new()
        {
            DefaultMaxFileSizeMb = 100,
            DefaultBatchSize = 1000,
            FuzzyMatchThreshold = 0.7,
        };

        // Assert
        options.DefaultMaxFileSizeMb.ShouldBe(100);
        options.DefaultBatchSize.ShouldBe(1000);
        options.FuzzyMatchThreshold.ShouldBe(0.7);
    }
}
