using Granit.Persistence.Interceptors;
using Granit.Templating.EntityFrameworkCore.Internal;
using Granit.Templating.Pipeline;
using Granit.Templating.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Granit.Templating.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core persistence for Granit templating.
/// </summary>
public static class TemplatingEntityFrameworkCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers EF Core persistence for the Granit template store.
    /// </summary>
    /// <remarks>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="IDocumentTemplateStoreReader"/> → <c>EfDocumentTemplateStore</c> (scoped)</item>
    ///   <item><see cref="IDocumentTemplateStoreWriter"/> → <c>EfDocumentTemplateStore</c> (scoped, shared instance)</item>
    ///   <item><see cref="ITemplateResolver"/> → <c>StoreTemplateResolver</c> (scoped, Priority=100)</item>
    ///   <item><see cref="ITemplateCategoryStoreReader"/> → <c>EfTemplateCategoryStore</c> (scoped)</item>
    ///   <item><see cref="ITemplateCategoryStoreWriter"/> → <c>EfTemplateCategoryStore</c> (scoped, shared instance)</item>
    /// </list>
    /// <para>
    /// Must be called after <c>AddGranitTemplatingWithScriban()</c> (or any other engine registration).
    /// </para>
    /// <para>
    /// <strong>Sovereignty:</strong> the connection string must point to a database hosted in Europe
    /// sur infrastructure souveraine. Never use a service subject to the US Cloud Act for health data.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitTemplatingEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddHybridCache();
        builder.Services.AddDbContextFactory<TemplatingDbContext>((sp, options) =>
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
        builder.Services.AddScoped<EfDocumentTemplateStore>();
        builder.Services.AddScoped<IDocumentTemplateStoreReader>(sp => sp.GetRequiredService<EfDocumentTemplateStore>());
        builder.Services.AddScoped<IDocumentTemplateStoreWriter>(sp => sp.GetRequiredService<EfDocumentTemplateStore>());
        builder.Services.AddScoped<ITemplateResolver, StoreTemplateResolver>();

        builder.Services.AddScoped<EfTemplateCategoryStore>();
        builder.Services.AddScoped<ITemplateCategoryStoreReader>(sp => sp.GetRequiredService<EfTemplateCategoryStore>());
        builder.Services.AddScoped<ITemplateCategoryStoreWriter>(sp => sp.GetRequiredService<EfTemplateCategoryStore>());

        return builder;
    }
}
