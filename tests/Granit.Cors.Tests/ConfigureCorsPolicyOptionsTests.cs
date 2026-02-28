using FluentAssertions;
using Granit.Cors.Internal;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.Cors.Tests;

public sealed class ConfigureCorsPolicyOptionsTests
{
    [Fact]
    public void Configure_WithExplicitOrigins_SetsOriginsOnDefaultPolicy()
    {
        GranitCorsOptions granitOptions = new()
        {
            AllowedOrigins = ["https://app.example.com", "https://admin.example.com"],
        };
        ConfigureCorsPolicyOptions sut = new(Options.Create(granitOptions));
        CorsOptions corsOptions = new();

        sut.Configure(corsOptions);

        CorsPolicy? policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain("https://app.example.com");
        policy.Origins.Should().Contain("https://admin.example.com");
        policy.AllowAnyOrigin.Should().BeFalse();
    }

    [Fact]
    public void Configure_WithWildcardOrigin_SetsAllowAnyOrigin()
    {
        GranitCorsOptions granitOptions = new() { AllowedOrigins = ["*"] };
        ConfigureCorsPolicyOptions sut = new(Options.Create(granitOptions));
        CorsOptions corsOptions = new();

        sut.Configure(corsOptions);

        CorsPolicy? policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.Should().NotBeNull();
        policy!.AllowAnyOrigin.Should().BeTrue();
    }

    [Fact]
    public void Configure_SetsAllowAnyHeaderAndMethod()
    {
        GranitCorsOptions granitOptions = new()
        {
            AllowedOrigins = ["https://app.example.com"],
        };
        ConfigureCorsPolicyOptions sut = new(Options.Create(granitOptions));
        CorsOptions corsOptions = new();

        sut.Configure(corsOptions);

        CorsPolicy? policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.Should().NotBeNull();
        policy!.AllowAnyHeader.Should().BeTrue();
        policy.AllowAnyMethod.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithAllowCredentials_SetsSupportsCredentials()
    {
        GranitCorsOptions granitOptions = new()
        {
            AllowedOrigins = ["https://app.example.com"],
            AllowCredentials = true,
        };
        ConfigureCorsPolicyOptions sut = new(Options.Create(granitOptions));
        CorsOptions corsOptions = new();

        sut.Configure(corsOptions);

        CorsPolicy? policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.Should().NotBeNull();
        policy!.SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithoutAllowCredentials_DoesNotSetSupportsCredentials()
    {
        GranitCorsOptions granitOptions = new()
        {
            AllowedOrigins = ["https://app.example.com"],
            AllowCredentials = false,
        };
        ConfigureCorsPolicyOptions sut = new(Options.Create(granitOptions));
        CorsOptions corsOptions = new();

        sut.Configure(corsOptions);

        CorsPolicy? policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.Should().NotBeNull();
        policy!.SupportsCredentials.Should().BeFalse();
    }
}
