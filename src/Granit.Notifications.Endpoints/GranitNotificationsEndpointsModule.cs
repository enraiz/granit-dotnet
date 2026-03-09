using Granit.Core.Modularity;
using Granit.Notifications.Endpoints.Validators;
using Granit.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Notifications.Endpoints;

/// <summary>
/// Granit module for notification HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes inbox, activity feed, preferences, subscriptions and push subscription
/// management routes via Minimal API endpoints.
/// </remarks>
[DependsOn(typeof(GranitNotificationsModule))]
public sealed class GranitNotificationsEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitValidatorsFromAssemblyContaining<UpdatePreferenceRequestValidator>();
}
