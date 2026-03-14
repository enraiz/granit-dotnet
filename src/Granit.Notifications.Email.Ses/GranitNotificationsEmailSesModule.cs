using Granit.Core.Modularity;
using Granit.Notifications.Email.Ses.Extensions;

namespace Granit.Notifications.Email.Ses;

/// <summary>Module for Amazon SES email provider.</summary>
public sealed class GranitNotificationsEmailSesModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitNotificationsEmailSes();
}
