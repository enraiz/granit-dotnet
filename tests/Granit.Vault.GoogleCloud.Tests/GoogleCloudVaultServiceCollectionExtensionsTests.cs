using Google.Cloud.Kms.V1;
using Google.Cloud.SecretManager.V1;
using Granit.Encryption;
using Granit.Vault.GoogleCloud.Extensions;
using Granit.Vault.GoogleCloud.HealthChecks;
using Granit.Vault.GoogleCloud.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Vault.GoogleCloud.Tests;

public sealed class GoogleCloudVaultServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersOptions()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<GoogleCloudVaultOptions>));
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersOptionsValidator()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<GoogleCloudVaultOptions>));
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersKmsClient()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(KeyManagementServiceClient) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersSecretManagerClient()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(SecretManagerServiceClient) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersTransitEncryption()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(ITransitEncryptionService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersStringEncryptionProvider()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IStringEncryptionProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersCredentialProvider()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IDatabaseCredentialProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_RegistersHostedService()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddGranitVaultGoogleCloud_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitVaultGoogleCloud();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitCloudKmsHealthCheck_RegistersHealthCheck()
    {
        ServiceCollection services = new();
        services.AddGranitVaultGoogleCloud();
        services.AddHealthChecks().AddGranitCloudKmsHealthCheck();

        services.ShouldContain(d =>
            d.ServiceType == typeof(CloudKmsHealthCheck));
    }
}
