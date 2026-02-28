using Shouldly;
using Granit.Features.AspNetCore;
using Granit.Features.Checker;
using Granit.Features.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Granit.Features.Tests.AspNetCore;

public sealed class RequiresFeatureFilterTests
{
    private static ActionExecutingContext BuildActionContext()
    {
        DefaultHttpContext httpContext = new();
        ActionContext actionContext = new(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }

    // -------------------------------------------------------------------------
    // RequiresFeatureAttribute — IFilterFactory
    // -------------------------------------------------------------------------

    [Fact]
    public void RequiresFeatureAttribute_CreateInstance_Returns_RequiresFeatureFilter()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        IServiceProvider sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IFeatureChecker)).Returns(checker);

        RequiresFeatureAttribute attribute = new("App.Feature");
        IFilterMetadata filter = attribute.CreateInstance(sp);

        filter.ShouldBeAssignableTo<IAsyncActionFilter>();
    }

    [Fact]
    public void RequiresFeatureAttribute_IsReusable_IsFalse()
    {
        RequiresFeatureAttribute attribute = new("App.Feature");
        attribute.IsReusable.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // RequiresFeatureFilter — OnActionExecutionAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnActionExecutionAsync_FeatureEnabled_CallsNext()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        checker.RequireEnabledAsync("App.Feature", Arg.Any<CancellationToken>())
               .Returns(Task.CompletedTask);

        RequiresFeatureFilter filter = new(checker, "App.Feature");
        ActionExecutingContext context = BuildActionContext();
        bool nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        };

        await filter.OnActionExecutionAsync(context, next);

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_FeatureDisabled_ThrowsAndDoesNotCallNext()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        checker.RequireEnabledAsync("App.Feature", Arg.Any<CancellationToken>())
               .ThrowsAsync(new FeatureNotEnabledException("App.Feature"));

        RequiresFeatureFilter filter = new(checker, "App.Feature");
        ActionExecutingContext context = BuildActionContext();
        bool nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        };

        Func<Task> act = () => filter.OnActionExecutionAsync(context, next);

        await Should.ThrowAsync<FeatureNotEnabledException>(act);
        nextCalled.ShouldBeFalse();
    }
}
