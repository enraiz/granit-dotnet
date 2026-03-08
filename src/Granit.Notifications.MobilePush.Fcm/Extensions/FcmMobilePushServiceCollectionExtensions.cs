using Microsoft.Extensions.DependencyInjection;

namespace Granit.Notifications.MobilePush.Fcm.Extensions;

/// <summary>Extension methods for the FCM mobile push provider.</summary>
public static class FcmMobilePushServiceCollectionExtensions
{
    private const string ProviderKey = "Fcm";
    private const string HttpClientName = "FcmPush";

    /// <summary>Registers the FCM mobile push sender as Keyed Service with key "Fcm".</summary>
    public static IServiceCollection AddGranitNotificationsMobilePushFcm(
        this IServiceCollection services,
        Action<FcmOptions>? configure = null)
    {
        services.AddOptions<FcmOptions>()
            .BindConfiguration(FcmOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddHttpClient(HttpClientName, (sp, client) =>
            {
                client.BaseAddress = new Uri("https://fcm.googleapis.com/");
                FcmOptions opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FcmOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            })
            .AddStandardResilienceHandler();

        services.AddKeyedSingleton<IMobilePushSender, FcmMobilePushSender>(ProviderKey);
        return services;
    }
}
