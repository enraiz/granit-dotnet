using FluentAssertions;
using Granit.Privacy.DataExport.Internal;
using Xunit;

namespace Granit.Privacy.Tests;

public sealed class DataProviderRegistryTests
{
    private readonly DataProviderRegistry _sut = new();

    [Fact]
    public void Register_AddsProvider()
    {
        _sut.Register("patients");

        _sut.Count.Should().Be(1);
        _sut.GetAll().Should().Contain("patients");
    }

    [Fact]
    public void Register_DuplicateName_ThrowsInvalidOperationException()
    {
        _sut.Register("patients");

        Action act = () => _sut.Register("patients");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void Register_IsCaseInsensitive()
    {
        _sut.Register("Patients");

        Action act = () => _sut.Register("patients");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Register_NullOrWhitespace_ThrowsArgumentException()
    {
        Action actNull = () => _sut.Register(null!);
        Action actEmpty = () => _sut.Register("");
        Action actWhitespace = () => _sut.Register("   ");

        actNull.Should().Throw<ArgumentException>();
        actEmpty.Should().Throw<ArgumentException>();
        actWhitespace.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        _sut.Register("patients");
        _sut.Register("billing");
        _sut.Register("appointments");

        IReadOnlyList<string> result = _sut.GetAll();

        result.Should().HaveCount(3);
        result.Should().Contain("patients");
        result.Should().Contain("billing");
        result.Should().Contain("appointments");
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        _sut.Register("a");
        _sut.Register("b");

        _sut.Count.Should().Be(2);
    }

    [Fact]
    public void Count_Empty_ReturnsZero()
    {
        _sut.Count.Should().Be(0);
    }
}
