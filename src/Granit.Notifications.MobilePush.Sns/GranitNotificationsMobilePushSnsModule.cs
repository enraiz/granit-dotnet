using Granit.Core.Modularity;
using Granit.Notifications.MobilePush.Sns.Extensions;

namespace Granit.Notifications.MobilePush.Sns;

/// <summary>Module for AWS SNS mobile push provider.</summary>
[DependsOn(typeof(GranitNotificationsMobilePushModule))]
public sealed class GranitNotificationsMobilePushSnsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitNotificationsMobilePushSns();
}
