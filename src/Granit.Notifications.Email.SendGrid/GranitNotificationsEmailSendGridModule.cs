using Granit.Core.Modularity;
using Granit.Notifications.Email.SendGrid.Extensions;

namespace Granit.Notifications.Email.SendGrid;

/// <summary>Module for SendGrid email provider.</summary>
[DependsOn(typeof(GranitNotificationsEmailModule))]
public sealed class GranitNotificationsEmailSendGridModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitNotificationsEmailSendGrid();
}
