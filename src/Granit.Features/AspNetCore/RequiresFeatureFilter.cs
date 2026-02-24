using Granit.Features.Checker;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Granit.Features.AspNetCore;

/// <summary>
/// MVC / Web API action filter that enforces a feature check before executing the action.
/// Created by <see cref="RequiresFeatureAttribute"/> via <c>IFilterFactory.CreateInstance</c>.
/// </summary>
internal sealed class RequiresFeatureFilter(
    IFeatureChecker featureChecker,
    string featureName) : IAsyncActionFilter
{
    private readonly IFeatureChecker _featureChecker = featureChecker;
    private readonly string _featureName = featureName;

    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await _featureChecker.RequireEnabledAsync(_featureName, context.HttpContext.RequestAborted);
        await next();
    }
}
