using FluentAssertions;
using Granit.Cors.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.Cors.Tests;

public sealed class GranitCorsOptionsValidatorTests
{
    [Fact]
    public void Validate_Production_WithExplicitOrigins_ReturnsSuccess()
    {
        IHostEnvironment environment = CreateEnvironment("Production");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new() { AllowedOrigins = ["https://app.example.com"] };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Production_WithWildcardOrigin_ReturnsFailed()
    {
        IHostEnvironment environment = CreateEnvironment("Production");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new() { AllowedOrigins = ["*"] };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("wildcard");
    }

    [Fact]
    public void Validate_Staging_WithWildcardOrigin_ReturnsFailed()
    {
        IHostEnvironment environment = CreateEnvironment("Staging");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new() { AllowedOrigins = ["*"] };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_Development_WithWildcardOrigin_ReturnsSuccess()
    {
        IHostEnvironment environment = CreateEnvironment("Development");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new() { AllowedOrigins = ["*"] };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrigins_ReturnsFailed()
    {
        IHostEnvironment environment = CreateEnvironment("Production");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new() { AllowedOrigins = [] };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(GranitCorsOptions.AllowedOrigins));
    }

    [Fact]
    public void Validate_AllowCredentialsWithWildcard_ReturnsFailed()
    {
        IHostEnvironment environment = CreateEnvironment("Development");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new()
        {
            AllowedOrigins = ["*"],
            AllowCredentials = true,
        };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(GranitCorsOptions.AllowCredentials));
    }

    [Fact]
    public void Validate_AllowCredentialsWithExplicitOrigins_ReturnsSuccess()
    {
        IHostEnvironment environment = CreateEnvironment("Production");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new()
        {
            AllowedOrigins = ["https://app.example.com"],
            AllowCredentials = true,
        };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Production_WildcardAndCredentials_ReportsMultipleErrors()
    {
        IHostEnvironment environment = CreateEnvironment("Production");
        GranitCorsOptionsValidator sut = new(environment);
        GranitCorsOptions options = new()
        {
            AllowedOrigins = ["*"],
            AllowCredentials = true,
        };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return environment;
    }
}
