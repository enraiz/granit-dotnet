using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Granit.Notifications.Email.Ses.Diagnostics;
using Granit.Notifications.Email.Ses.Internal;
using Granit.Notifications.Email.Ses.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Email.Ses.Extensions;

/// <summary>Extension methods for the Amazon SES email provider.</summary>
public static class SesEmailServiceCollectionExtensions
{
    /// <summary>Registers the Amazon SES email sender as Keyed Service with key "Ses".</summary>
    public static IServiceCollection AddGranitNotificationsEmailSes(
        this IServiceCollection services,
        Action<SesOptions>? configure = null)
    {
        services.AddOptions<SesOptions>()
            .BindConfiguration(SesOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SesOptions>, SesOptionsValidator>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<Func<ISesTransport>>(sp =>
        {
            SesOptions opts = sp.GetRequiredService<IOptions<SesOptions>>().Value;
            return () =>
            {
                var config = new AmazonSimpleEmailServiceV2Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(opts.Region),
                    Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds),
                };

                IAmazonSimpleEmailServiceV2 client = opts.AccessKeyId is not null
                    ? new AmazonSimpleEmailServiceV2Client(
                        new BasicAWSCredentials(opts.AccessKeyId, opts.SecretAccessKey), config)
                    : new AmazonSimpleEmailServiceV2Client(config);

                return new AwsSesTransport(client);
            };
        });

        services.AddKeyedSingleton<IEmailSender, SesEmailSender>("Ses");

        NotificationsEmailSesActivitySource.Source.GetType(); // ensure static init

        return services;
    }
}
