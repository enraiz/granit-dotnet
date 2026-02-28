using Granit.BackgroundJobs.Internal;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.BackgroundJobs.Tests;

public sealed class BackgroundJobsOptionsValidatorTests
{
    private readonly BackgroundJobsOptionsValidator _sut = new();

    [Fact]
    public void Validate_InMemoryMode_ReturnsSuccess()
    {
        // Arrange
        BackgroundJobsOptions options = new() { Mode = JobStoreMode.InMemory };

        // Act
        ValidateOptionsResult result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DurableModeWithConnectionString_ReturnsSuccess()
    {
        // Arrange
        BackgroundJobsOptions options = new()
        {
            Mode = JobStoreMode.Durable,
            ConnectionString = "Host=localhost;Database=granit;"
        };

        // Act
        ValidateOptionsResult result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_DurableModeWithoutConnectionString_ReturnsFailed(string cs)
    {
        // Arrange
        BackgroundJobsOptions options = new()
        {
            Mode = JobStoreMode.Durable,
            ConnectionString = cs
        };

        // Act
        ValidateOptionsResult result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(BackgroundJobsOptions.ConnectionString));
    }
}
