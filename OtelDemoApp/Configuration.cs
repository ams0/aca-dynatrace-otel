using System;
using System.Collections.Generic;

namespace OtelDemoApp;

public class Configuration
{
    public string OtelEndpoint { get; }
    public int IntervalSeconds { get; }
    public int IntervalSecondsLogs { get; }
    public int IntervalSecondsMetrics { get; }
    public int IntervalSecondsTraces { get; }
    public bool EnableLogs { get; }
    public bool EnableMetrics { get; }
    public bool EnableTraces { get; }
    public string CustomLabel { get; }
    public Dictionary<string, string> HttpHeaders { get; }
    public string ExportProtocol { get; }

    public Configuration()
    {
        // Default values
        OtelEndpoint = Environment.GetEnvironmentVariable("OTEL_ENDPOINT") ?? "http://localhost:4317";
        IntervalSeconds = int.Parse(Environment.GetEnvironmentVariable("OTEL_INTERVAL_SECONDS") ?? "5");
        IntervalSecondsLogs = int.TryParse(Environment.GetEnvironmentVariable("OTEL_INTERVAL_SECONDS_LOGS"), out var il) ? il : IntervalSeconds;
        IntervalSecondsMetrics = int.TryParse(Environment.GetEnvironmentVariable("OTEL_INTERVAL_SECONDS_METRICS"), out var im) ? im : IntervalSeconds;
        IntervalSecondsTraces = int.TryParse(Environment.GetEnvironmentVariable("OTEL_INTERVAL_SECONDS_TRACES"), out var it) ? it : IntervalSeconds;
        EnableLogs = !string.Equals(Environment.GetEnvironmentVariable("OTEL_ENABLE_LOGS"), "false", StringComparison.OrdinalIgnoreCase);
        EnableMetrics = !string.Equals(Environment.GetEnvironmentVariable("OTEL_ENABLE_METRICS"), "false", StringComparison.OrdinalIgnoreCase);
        EnableTraces = !string.Equals(Environment.GetEnvironmentVariable("OTEL_ENABLE_TRACES"), "false", StringComparison.OrdinalIgnoreCase);
        CustomLabel = Environment.GetEnvironmentVariable("OTEL_CUSTOM_LABEL") ?? "demo-app";
        var protocol = Environment.GetEnvironmentVariable("OTEL_EXPORT_PROTOCOL");
        ExportProtocol = string.IsNullOrWhiteSpace(protocol) ? "protobuf" : protocol.ToLowerInvariant();

        var headersRaw = Environment.GetEnvironmentVariable("OTEL_HTTP_HEADERS");
        HttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(headersRaw))
        {
            var pairs = headersRaw.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2)
                {
                    HttpHeaders[kv[0].Trim()] = kv[1].Trim();
                }
            }
        }
    }
}