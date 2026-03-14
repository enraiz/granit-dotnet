using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Granit.Notifications.Sms.Sns.Diagnostics;
using Granit.Notifications.Sms.Sns.HealthChecks;
using Granit.Notifications.Sms.Sns.Internal;
using Granit.Notifications.Sms.Sns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Sms.Sns.Extensions;

/// <summary>Extension methods for the AWS SNS SMS provider.</summary>
public static class SnsSmsServiceCollectionExtensions
{
    /// <summary>Registers the SNS SMS sender as Keyed Service with key "Sns".</summary>
    public static IServiceCollection AddGranitNotificationsSmsSns(
        this IServiceCollection services,
        Action<SnsSmsOptions>? configure = null)
    {
        services.AddOptions<SnsSmsOptions>()
            .BindConfiguration(SnsSmsOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SnsSmsOptions>, SnsSmsOptionsValidator>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<ISnsSmsTransport>(sp =>
        {
            SnsSmsOptions opts = sp.GetRequiredService<IOptions<SnsSmsOptions>>().Value;
            var region = RegionEndpoint.GetBySystemName(opts.Region);

            IAmazonSimpleNotificationService client = !string.IsNullOrEmpty(opts.AccessKeyId)
                ? new AmazonSimpleNotificationServiceClient(
                    new BasicAWSCredentials(opts.AccessKeyId, opts.SecretAccessKey), region)
                : new AmazonSimpleNotificationServiceClient(region);

            return new AwsSnsSmsTransport(client);
        });

        services.AddKeyedSingleton<ISmsSender, SnsSmsSender>("Sns");

        _ = NotificationsSmsSnsActivitySource.Source; // ensure static init

        return services;
    }

    /// <summary>
    /// Adds the SNS SMS health check (tags: <c>readiness</c>, <c>startup</c>).
    /// </summary>
    public static IHealthChecksBuilder AddGranitSnsSmsHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "sns-sms",
        HealthStatus? failureStatus = null,
        TimeSpan? timeout = null)
    {
        builder.Services.AddSingleton<SnsSmsHealthCheck>();

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredService<SnsSmsHealthCheck>(),
            failureStatus,
            ["readiness", "startup"],
            timeout));
    }
}
