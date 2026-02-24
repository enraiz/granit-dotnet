using Granit.Observability.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Granit.Observability.Extensions;

/// <summary>
/// Extensions for configuring full observability (logs, traces, metrics).
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds Serilog (structured logs) and OpenTelemetry (traces + metrics)
    /// with OTLP export to the LGTM stack.
    /// </summary>
    public static IHostApplicationBuilder AddGranitObservability(
        this IHostApplicationBuilder builder)
    {
        IConfigurationSection section = builder.Configuration.GetSection(ObservabilityOptions.SectionName);
        builder.Services.Configure<ObservabilityOptions>(section);

        ObservabilityOptions options = section.Get<ObservabilityOptions>() ?? new ObservabilityOptions();

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
                        {
                            Microsoft.AspNetCore.Http.PathString path = httpContext.Request.Path;
                            // Exclude /health/* (liveness, readiness, startup) and /healthz (legacy).
                            // StartsWithSegments is segment-safe: /healthcare/... is NOT excluded.
                            return !path.StartsWithSegments("/health") && path != "/healthz";
                        };
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
