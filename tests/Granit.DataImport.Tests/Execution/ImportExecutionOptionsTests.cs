using Granit.DataImport.Execution;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Tests.Execution;

public sealed class ImportExecutionOptionsTests
{
    [Fact]
    public void Default_batch_size_is_500()
    {
        ImportExecutionOptions options = new();
        options.BatchSize.ShouldBe(500);
    }

    [Fact]
    public void Default_dry_run_is_false()
    {
        ImportExecutionOptions options = new();
        options.DryRun.ShouldBeFalse();
    }

    [Fact]
    public void Default_error_behavior_is_SkipErrors()
    {
        ImportExecutionOptions options = new();
        options.ErrorBehavior.ShouldBe(ImportErrorBehavior.SkipErrors);
    }

    [Fact]
    public void Can_set_custom_values()
    {
        // Arrange & Act
        ImportExecutionOptions options = new()
        {
            BatchSize = 100,
            DryRun = true,
            ErrorBehavior = ImportErrorBehavior.FailFast,
        };

        // Assert
        options.BatchSize.ShouldBe(100);
        options.DryRun.ShouldBeTrue();
        options.ErrorBehavior.ShouldBe(ImportErrorBehavior.FailFast);
    }

    [Fact]
    public void ImportErrorBehavior_has_three_values()
    {
        Enum.GetValues<ImportErrorBehavior>().Length.ShouldBe(3);
    }
}
