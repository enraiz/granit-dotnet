using Microsoft.Extensions.DependencyInjection;

namespace Granit.Notifications.Email.Smtp.Extensions;

/// <summary>Extension methods for the MailKit SMTP email provider.</summary>
public static class SmtpEmailServiceCollectionExtensions
{
    /// <summary>Registers the MailKit SMTP email sender as Keyed Service with key "Smtp".</summary>
    public static IServiceCollection AddGranitNotificationsEmailSmtp(
        this IServiceCollection services,
        Action<SmtpOptions>? configure = null)
    {
        services.AddOptions<SmtpOptions>()
            .BindConfiguration(SmtpOptions.SectionName)
            .ValidateOnStart();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddKeyedSingleton<IEmailSender, MailKitEmailSender>("Smtp");
        return services;
    }
}
