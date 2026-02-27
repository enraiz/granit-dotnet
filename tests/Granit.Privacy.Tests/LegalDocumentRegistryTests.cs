using FluentAssertions;
using Granit.Privacy.LegalAgreements;
using Granit.Privacy.LegalAgreements.Internal;
using Xunit;

namespace Granit.Privacy.Tests;

public sealed class LegalDocumentRegistryTests
{
    private readonly LegalDocumentRegistry _sut = new();

    [Fact]
    public void Register_AddsDocument()
    {
        _sut.Register(new LegalDocumentDefinition("privacy-policy", "1.0.0", "Privacy Policy"));

        _sut.GetDefinition("privacy-policy").Should().NotBeNull();
    }

    [Fact]
    public void Register_DuplicateId_ThrowsInvalidOperationException()
    {
        _sut.Register(new LegalDocumentDefinition("privacy-policy", "1.0.0", "Privacy Policy"));

        Action act = () => _sut.Register(new LegalDocumentDefinition("privacy-policy", "2.0.0", "Privacy Policy v2"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void GetDefinition_UnknownDocument_ReturnsNull()
    {
        _sut.GetDefinition("unknown").Should().BeNull();
    }

    [Fact]
    public void GetDefinition_IsCaseInsensitive()
    {
        _sut.Register(new LegalDocumentDefinition("Privacy-Policy", "1.0.0", "Privacy Policy"));

        _sut.GetDefinition("privacy-policy").Should().NotBeNull();
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        _sut.Register(new LegalDocumentDefinition("privacy-policy", "1.0.0", "Privacy Policy"));
        _sut.Register(new LegalDocumentDefinition("terms", "1.0.0", "Terms of Service"));

        _sut.GetAll().Should().HaveCount(2);
    }
}
