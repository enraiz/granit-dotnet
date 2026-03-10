using Granit.Persistence.Interceptors;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Webhooks.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for enabling EF Core persistence in Granit.Webhooks.
/// </summary>
public static class WebhooksEfCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Replaces the default InMemory/no-op stores with durable EF Core implementations
    /// backed by a PostgreSQL database hosted in Europe (OVHcloud FR).
    /// </summary>
    /// <remarks>
    /// Must be called after <c>AddGranitWebhooks()</c>.
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="EfWebhookSubscriptionStore"/> — replaces <c>InMemoryWebhookSubscriptionStore</c> for both <see cref="IWebhookSubscriptionReader"/> and <see cref="IWebhookSubscriptionWriter"/>.</item>
    ///   <item><see cref="EfWebhookDeliveryStore"/> — replaces <c>NullWebhookDeliveryWriter</c> (enables HDS audit trail).</item>
    ///   <item><see cref="WebhooksDbContext"/> — registered via <c>IDbContextFactory</c> for thread-safe usage in Wolverine handlers.</item>
    /// </list>
    /// <para>
    /// SOVEREIGNTY: The connection string must point to a database hosted in Europe (OVHcloud FR).
    /// Never use AWS RDS, Azure SQL, or Google Cloud SQL for health data.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration (provider + connection string).</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitWebhooksEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<WebhooksDbContext>((sp, options) =>
        {
            configure(options);

            AuditedEntityInterceptor? auditInterceptor =
                sp.GetService<AuditedEntityInterceptor>();
            if (auditInterceptor is not null)
            {
                options.AddInterceptors(auditInterceptor);
            }

            SoftDeleteInterceptor? softDeleteInterceptor =
                sp.GetService<SoftDeleteInterceptor>();
            if (softDeleteInterceptor is not null)
            {
                options.AddInterceptors(softDeleteInterceptor);
            }
        }, ServiceLifetime.Scoped);

        builder.Services.AddSingleton<EfWebhookSubscriptionStore>();
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IWebhookSubscriptionReader>(sp => sp.GetRequiredService<EfWebhookSubscriptionStore>()));
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IWebhookSubscriptionWriter>(sp => sp.GetRequiredService<EfWebhookSubscriptionStore>()));

        builder.Services.Replace(
            ServiceDescriptor.Scoped<IWebhookDeliveryWriter, EfWebhookDeliveryStore>());
        builder.Services.Replace(
            ServiceDescriptor.Scoped<IWebhookDeliveryReader, EfWebhookDeliveryStore>());

        return builder;
    }
}
