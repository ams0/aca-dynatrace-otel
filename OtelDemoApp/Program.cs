using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtelDemoApp;

var config = new Configuration();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(config);
        services.AddSingleton<TelemetryService>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: "OtelDemoApp"))
            .WithTracing(tracing => tracing
                .AddSource("OtelDemoApp")
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(config.OtelEndpoint)))
            .WithMetrics(metrics => metrics
                .AddMeter("OtelDemoApp")
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(config.OtelEndpoint)));

        // Configure logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            // Add console logging
            logging.AddConsole();
            // Add OpenTelemetry logging
            logging.AddOpenTelemetry(options =>
            {
                options.AddOtlpExporter(opts => opts.Endpoint = new Uri(config.OtelEndpoint));
            });
        });
    });

var host = builder.Build();

var telemetryService = host.Services.GetRequiredService<TelemetryService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting application...");

try
{
    var telemetryTask = telemetryService.StartAsync();
    await host.RunAsync();
    await telemetryTask;
}
catch (Exception ex)
{
    logger.LogError(ex, "Application terminated unexpectedly");
}
finally
{
    telemetryService.Stop();
    await host.StopAsync();
}
