using Granit.Core.Modularity;
using Granit.Notifications.Sms.Sns.Extensions;

namespace Granit.Notifications.Sms.Sns;

/// <summary>Module for AWS SNS SMS provider.</summary>
[DependsOn(typeof(GranitNotificationsSmsModule))]
public sealed class GranitNotificationsSmsSnsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitNotificationsSmsSns();
}
