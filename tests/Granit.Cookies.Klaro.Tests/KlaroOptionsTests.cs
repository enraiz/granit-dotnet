using System.ComponentModel.DataAnnotations;
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

    [Fact]
    public void ServiceMappings_Required_ValidationFails_WhenNull()
    {
        KlaroOptions options = new() { ServiceMappings = null! };

        List<ValidationResult> results = [];
        bool isValid = Validator.TryValidateObject(
            options, new ValidationContext(options), results, validateAllProperties: true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(KlaroOptions.ServiceMappings)));
    }
}
