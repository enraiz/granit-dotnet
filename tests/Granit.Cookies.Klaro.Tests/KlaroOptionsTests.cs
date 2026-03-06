using Granit.Cookies.Klaro.Options;
using Shouldly;
using Xunit;

namespace Granit.Cookies.Klaro.Tests;

public sealed class KlaroOptionsTests
{
    [Fact]
    public void SectionName_IsKlaro() =>
        KlaroOptions.SectionName.ShouldBe("Klaro");

    [Fact]
    public void DefaultCookieName_IsKlaro()
    {
        KlaroOptions options = new();

        options.CookieName.ShouldBe("klaro");
    }
}
