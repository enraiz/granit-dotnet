using Granit.Notifications.Brevo.Internal;
using Granit.Notifications.Brevo.Options;
using Granit.Notifications.Email;
using Granit.Notifications.Sms;
using Granit.Notifications.WhatsApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Granit.Notifications.Brevo.Extensions;

/// <summary>Extension methods for Brevo notification provider registration.</summary>
public static class BrevoNotificationsServiceCollectionExtensions
{
    private const string ProviderKey = "Brevo";

    /// <summary>
    /// Registers the unified Brevo provider as Keyed Services for Email, SMS, and WhatsApp.
    /// </summary>
    public static IServiceCollection AddGranitNotificationsBrevo(
        this IServiceCollection services,
        Action<BrevoOptions>? configure = null)
    {
        services.AddOptions<BrevoOptions>()
            .BindConfiguration(BrevoOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddHttpClient(ProviderKey, (sp, client) =>
            {
                BrevoOptions opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BrevoOptions>>().Value;
                client.BaseAddress = new Uri(string.Concat(opts.BaseUrl.TrimEnd('/'), "/"));
                client.DefaultRequestHeaders.Add("api-key", opts.ApiKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            })
            .AddStandardResilienceHandler();

        services.AddSingleton<BrevoNotificationProvider>();
        services.AddKeyedSingleton<IEmailSender>(
            ProviderKey, (sp, _) => sp.GetRequiredService<BrevoNotificationProvider>());
        services.AddKeyedSingleton<ISmsSender>(
            ProviderKey, (sp, _) => sp.GetRequiredService<BrevoNotificationProvider>());
        services.AddKeyedSingleton<IWhatsAppSender>(
            ProviderKey, (sp, _) => sp.GetRequiredService<BrevoNotificationProvider>());

        return services;
    }
}
