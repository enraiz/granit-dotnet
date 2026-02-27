using FluentAssertions;
using Granit.Cookies.Internal;
using Xunit;

namespace Granit.Cookies.Tests;

public sealed class CookieRegistryTests
{
    private readonly CookieRegistry _sut = new();

    private static CookieDefinition CreateDefinition(
        string name = "test_cookie",
        CookieCategory category = CookieCategory.Analytics) =>
        new(name, category, 365, true, "Test purpose");

    [Fact]
    public void Register_AddsDefinition()
    {
        CookieDefinition definition = CreateDefinition();

        _sut.Register(definition);

        _sut.IsRegistered("test_cookie").Should().BeTrue();
    }

    [Fact]
    public void Register_DuplicateName_ThrowsInvalidOperationException()
    {
        CookieDefinition definition = CreateDefinition();
        _sut.Register(definition);

        Action act = () => _sut.Register(definition);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void GetDefinition_ExistingCookie_ReturnsDefinition()
    {
        CookieDefinition definition = CreateDefinition();
        _sut.Register(definition);

        CookieDefinition? result = _sut.GetDefinition("test_cookie");

        result.Should().Be(definition);
    }

    [Fact]
    public void GetDefinition_UnknownCookie_ReturnsNull()
    {
        CookieDefinition? result = _sut.GetDefinition("unknown");

        result.Should().BeNull();
    }

    [Fact]
    public void GetDefinition_IsCaseInsensitive()
    {
        CookieDefinition definition = CreateDefinition();
        _sut.Register(definition);

        CookieDefinition? result = _sut.GetDefinition("TEST_COOKIE");

        result.Should().Be(definition);
    }

    [Fact]
    public void GetByCategory_ReturnsMatchingCookies()
    {
        _sut.Register(new("analytics_1", CookieCategory.Analytics, 365, false, "Analytics 1"));
        _sut.Register(new("analytics_2", CookieCategory.Analytics, 365, false, "Analytics 2"));
        _sut.Register(new("session", CookieCategory.StrictlyNecessary, 1, true, "Session"));

        IReadOnlyList<CookieDefinition> result = _sut.GetByCategory(CookieCategory.Analytics);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.Category.Should().Be(CookieCategory.Analytics));
    }

    [Fact]
    public void GetByCategory_NoMatches_ReturnsEmpty()
    {
        _sut.Register(CreateDefinition());

        IReadOnlyList<CookieDefinition> result = _sut.GetByCategory(CookieCategory.Marketing);

        result.Should().BeEmpty();
    }

    [Fact]
    public void IsRegistered_ReturnsTrueForRegistered()
    {
        _sut.Register(CreateDefinition());

        _sut.IsRegistered("test_cookie").Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_ReturnsFalseForUnknown() =>
        _sut.IsRegistered("unknown").Should().BeFalse();

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        _sut.Register(new("cookie_1", CookieCategory.Analytics, 365, false, "Cookie 1"));
        _sut.Register(new("cookie_2", CookieCategory.Preferences, 180, true, "Cookie 2"));
        _sut.Register(new("cookie_3", CookieCategory.StrictlyNecessary, 1, true, "Cookie 3"));

        IReadOnlyList<CookieDefinition> result = _sut.GetAll();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void Register_NullDefinition_ThrowsArgumentNullException()
    {
        Action act = () => _sut.Register(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
