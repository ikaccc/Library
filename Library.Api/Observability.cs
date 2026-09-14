using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Library.Api;

internal static class Observability
{
    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        var version = typeof(Observability).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Configuration["Otel:ServiceName"] ?? "Unknown", serviceVersion: version))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["Otel:Endpoint"]))
        {
            openTelemetry.UseOtlpExporter();
        }

        return builder;
    }
}
