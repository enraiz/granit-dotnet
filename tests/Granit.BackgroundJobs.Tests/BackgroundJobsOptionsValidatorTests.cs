using FluentAssertions;
using Granit.BackgroundJobs.Internal;
using Microsoft.Extensions.Options;
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
        result.Succeeded.Should().BeTrue();
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
        result.Succeeded.Should().BeTrue();
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
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(BackgroundJobsOptions.ConnectionString));
    }
}
