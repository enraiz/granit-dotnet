using FluentAssertions;
using Granit.Privacy.DataExport;
using Granit.Privacy.Extensions;
using Granit.Privacy.LegalAgreements;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Granit.Privacy.Tests;

public sealed class PrivacyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitPrivacy_RegistersServices()
    {
        ServiceCollection services = new();
        services.AddScoped(_ => Substitute.For<ILegalAgreementStore>());
        services.AddGranitPrivacy(privacy =>
        {
            privacy.RegisterDataProvider("patients");
            privacy.RegisterDocument("privacy-policy", "1.0.0", "Privacy Policy");
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IDataProviderRegistry>().Should().NotBeNull();
        provider.GetService<ILegalDocumentRegistry>().Should().NotBeNull();
        provider.GetService<ILegalAgreementChecker>().Should().NotBeNull();
    }

    [Fact]
    public void AddGranitPrivacy_RegistersDataProviders()
    {
        ServiceCollection services = new();
        services.AddGranitPrivacy(privacy =>
        {
            privacy.RegisterDataProvider("patients");
            privacy.RegisterDataProvider("billing");
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IDataProviderRegistry? registry = provider.GetService<IDataProviderRegistry>();

        registry.Should().NotBeNull();
        registry!.Count.Should().Be(2);
        registry.GetAll().Should().Contain("patients");
        registry.GetAll().Should().Contain("billing");
    }

    [Fact]
    public void AddGranitPrivacy_RegistersLegalDocuments()
    {
        ServiceCollection services = new();
        services.AddGranitPrivacy(privacy =>
        {
            privacy.RegisterDocument("privacy-policy", "2.0.0", "Privacy Policy");
            privacy.RegisterDocument("terms", "1.0.0", "Terms of Service");
        });

        ServiceProvider provider = services.BuildServiceProvider();
        ILegalDocumentRegistry? registry = provider.GetService<ILegalDocumentRegistry>();

        registry.Should().NotBeNull();
        registry!.GetAll().Should().HaveCount(2);
        registry.GetDefinition("privacy-policy")!.CurrentVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void AddGranitPrivacy_NullConfigure_ThrowsArgumentNullException()
    {
        ServiceCollection services = new();

        Action act = () => services.AddGranitPrivacy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
