using Shouldly;
using Granit.Core.Localization;
using Granit.Localization;
using Xunit;

namespace Granit.Features.Tests;

public sealed class FeaturesLocalizationResourceTests
{
    [Fact]
    public void LocalizationResourceNameAttribute_HasName_Features()
    {
        LocalizationResourceNameAttribute? attribute =
            (LocalizationResourceNameAttribute?)Attribute.GetCustomAttribute(
                typeof(FeaturesLocalizationResource),
                typeof(LocalizationResourceNameAttribute));

        attribute.ShouldNotBeNull();
        attribute!.Name.ShouldBe("Features");
    }

    [Fact]
    public void InheritResourceAttribute_InheritsFrom_GranitLocalizationResource()
    {
        InheritResourceAttribute? attribute =
            (InheritResourceAttribute?)Attribute.GetCustomAttribute(
                typeof(FeaturesLocalizationResource),
                typeof(InheritResourceAttribute));

        attribute.ShouldNotBeNull();
        attribute!.BaseResourceTypes.ShouldContain(typeof(GranitLocalizationResource));
    }

    [Fact]
    public void Class_IsSealed()
    {
        typeof(FeaturesLocalizationResource).IsSealed.ShouldBeTrue();
    }
}
