using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Granit.Notifications.MobilePush.Sns.Diagnostics;
using Granit.Notifications.MobilePush.Sns.HealthChecks;
using Granit.Notifications.MobilePush.Sns.Internal;
using Granit.Notifications.MobilePush.Sns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.MobilePush.Sns.Extensions;

/// <summary>Extension methods for the AWS SNS mobile push provider.</summary>
public static class SnsMobilePushServiceCollectionExtensions
{
    /// <summary>Registers the SNS mobile push sender as Keyed Service with key "Sns".</summary>
    public static IServiceCollection AddGranitNotificationsMobilePushSns(
        this IServiceCollection services,
        Action<SnsMobilePushOptions>? configure = null)
    {
        services.AddOptions<SnsMobilePushOptions>()
            .BindConfiguration(SnsMobilePushOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SnsMobilePushOptions>, SnsMobilePushOptionsValidator>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<ISnsMobilePushTransport>(sp =>
        {
            SnsMobilePushOptions opts = sp.GetRequiredService<IOptions<SnsMobilePushOptions>>().Value;
            var region = RegionEndpoint.GetBySystemName(opts.Region);

            IAmazonSimpleNotificationService client = !string.IsNullOrEmpty(opts.AccessKeyId)
                ? new AmazonSimpleNotificationServiceClient(
                    new BasicAWSCredentials(opts.AccessKeyId, opts.SecretAccessKey), region)
                : new AmazonSimpleNotificationServiceClient(region);

            return new AwsSnsMobilePushTransport(client);
        });

        services.AddKeyedSingleton<IMobilePushSender, SnsMobilePushSender>("Sns");

        _ = NotificationsMobilePushSnsActivitySource.Source; // ensure static init

        return services;
    }

    /// <summary>
    /// Adds the SNS mobile push health check (tags: <c>readiness</c>, <c>startup</c>).
    /// </summary>
    public static IHealthChecksBuilder AddGranitSnsMobilePushHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "sns-mobile-push",
        HealthStatus? failureStatus = null,
        TimeSpan? timeout = null)
    {
        builder.Services.AddSingleton<SnsMobilePushHealthCheck>();

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredService<SnsMobilePushHealthCheck>(),
            failureStatus,
            ["readiness", "startup"],
            timeout));
    }
}
