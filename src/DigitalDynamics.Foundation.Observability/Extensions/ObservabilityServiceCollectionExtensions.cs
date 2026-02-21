// =============================================================================
// ObservabilityServiceCollectionExtensions - Serilog + OpenTelemetry → OTLP
// =============================================================================
// Configure Serilog (logs structurés) et OpenTelemetry (traces + métriques)
// avec export vers un collecteur OTLP (stack LGTM : Loki/Grafana/Tempo/Mimir).
//
// Usage :
//   builder.AddFoundationObservability();
// =============================================================================

using DigitalDynamics.Foundation.Observability.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace DigitalDynamics.Foundation.Observability.Extensions;

/// <summary>
/// Extensions pour configurer l'observabilité complète (logs, traces, métriques).
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute Serilog (logs structurés) et OpenTelemetry (traces + métriques)
    /// avec export OTLP vers la stack LGTM.
    /// </summary>
    public static IHostApplicationBuilder AddFoundationObservability(
        this IHostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(ObservabilityOptions.SectionName);
        builder.Services.Configure<ObservabilityOptions>(section);

        var options = section.Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        ConfigureSerilog(builder, options);
        ConfigureOpenTelemetry(builder, options);

        return builder;
    }

    private static void ConfigureSerilog(IHostApplicationBuilder builder, ObservabilityOptions options)
    {
        builder.Services.AddSerilog(config =>
        {
            config
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", options.ServiceName)
                .Enrich.WithProperty("ServiceVersion", options.ServiceVersion)
                .Enrich.WithProperty("Environment", options.Environment)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                .WriteTo.OpenTelemetry(otel =>
                {
                    otel.Endpoint = options.OtlpEndpoint;
                    otel.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = options.ServiceName,
                        ["service.version"] = options.ServiceVersion,
                        ["service.namespace"] = options.ServiceNamespace,
                        ["deployment.environment"] = options.Environment
                    };
                });
        });
    }

    private static void ConfigureOpenTelemetry(IHostApplicationBuilder builder, ObservabilityOptions options)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion,
                serviceNamespace: options.ServiceNamespace);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion,
                serviceNamespace: options.ServiceNamespace))
            .WithTracing(tracing =>
            {
                if (!options.EnableTracing)
                {
                    return;
                }

                tracing
                    .AddAspNetCoreInstrumentation(aspnet =>
                    {
                        aspnet.RecordException = true;
                        aspnet.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/healthz");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
            })
            .WithMetrics(metrics =>
            {
                if (!options.EnableMetrics)
                {
                    return;
                }

                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
            });
    }
}
