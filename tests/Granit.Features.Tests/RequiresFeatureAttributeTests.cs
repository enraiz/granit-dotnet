using Granit.Features.AspNetCore;
using Granit.Features.Checker;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Features.Tests;

public sealed class RequiresFeatureAttributeTests
{
    // -------------------------------------------------------------------------
    // Constructor — FeatureName property
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_SetsFeatureName()
    {
        RequiresFeatureAttribute attribute = new("App.VideoConsultation");

        attribute.FeatureName.ShouldBe("App.VideoConsultation");
    }

    // -------------------------------------------------------------------------
    // AttributeUsage metadata
    // -------------------------------------------------------------------------

    [Fact]
    public void AttributeUsage_AllowsMultiple()
    {
        AttributeUsageAttribute? usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(RequiresFeatureAttribute), typeof(AttributeUsageAttribute));

        usage.ShouldNotBeNull();
        usage!.AllowMultiple.ShouldBeTrue();
    }

    [Fact]
    public void AttributeUsage_IsInherited()
    {
        AttributeUsageAttribute? usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(RequiresFeatureAttribute), typeof(AttributeUsageAttribute));

        usage.ShouldNotBeNull();
        usage!.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void AttributeUsage_TargetsClassAndMethod()
    {
        AttributeUsageAttribute? usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(RequiresFeatureAttribute), typeof(AttributeUsageAttribute));

        usage.ShouldNotBeNull();
        usage!.ValidOn.ShouldBe(AttributeTargets.Class | AttributeTargets.Method);
    }

    // -------------------------------------------------------------------------
    // IFilterFactory — IsReusable
    // -------------------------------------------------------------------------

    [Fact]
    public void IsReusable_IsFalse()
    {
        RequiresFeatureAttribute attribute = new("App.Feature");

        attribute.IsReusable.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // IFilterFactory — CreateInstance
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateInstance_Returns_IAsyncActionFilter()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        ServiceCollection services = new();
        services.AddSingleton(checker);
        ServiceProvider sp = services.BuildServiceProvider();

        RequiresFeatureAttribute attribute = new("App.Feature");
        IFilterMetadata filter = attribute.CreateInstance(sp);

        filter.ShouldBeAssignableTo<IAsyncActionFilter>();
    }

    [Fact]
    public void CreateInstance_Returns_RequiresFeatureFilter()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        ServiceCollection services = new();
        services.AddSingleton(checker);
        ServiceProvider sp = services.BuildServiceProvider();

        RequiresFeatureAttribute attribute = new("App.Feature");
        IFilterMetadata filter = attribute.CreateInstance(sp);

        filter.ShouldBeOfType<RequiresFeatureFilter>();
    }

    // -------------------------------------------------------------------------
    // Multiple attributes on same class
    // -------------------------------------------------------------------------

    [RequiresFeature("App.FeatureA")]
    [RequiresFeature("App.FeatureB")]
    private sealed class MultiAttributeTarget;

    [Fact]
    public void MultipleAttributes_AreAllRetrievable()
    {
        object[] attributes = typeof(MultiAttributeTarget)
            .GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true);

        attributes.Length.ShouldBe(2);
        IEnumerable<string> featureNames = attributes.Cast<RequiresFeatureAttribute>()
            .Select(a => a.FeatureName);
        featureNames.ShouldContain("App.FeatureA");
        featureNames.ShouldContain("App.FeatureB");
    }
}
