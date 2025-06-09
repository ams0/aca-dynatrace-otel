using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OtelDemoApp;

public class TelemetryService : IDisposable
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly Configuration _config;
    private readonly Meter _meter;
    private readonly Counter<long> _operationCounter;
    private readonly ActivitySource _activitySource;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly HttpClient _httpClient;
    private bool _lastConnectionSuccessful = true;
    private DateTime _lastConnectionAttempt = DateTime.MinValue;

    public TelemetryService(ILogger<TelemetryService> logger, Configuration config)
    {
        _logger = logger;
        _config = config;
        _meter = new Meter("OtelDemoApp");
        _operationCounter = _meter.CreateCounter<long>("demo_operations_total", "Total number of operations");
        _activitySource = new ActivitySource("OtelDemoApp");
        _cancellationTokenSource = new CancellationTokenSource();
        _httpClient = new HttpClient();

        // Log initial connection attempt
        LogConnectionStatus(true, "Initial connection attempt");
    }

    private void LogConnectionStatus(bool success, string message)
    {
        // Only log if the status has changed or it's been more than 5 minutes since the last log
        if (success != _lastConnectionSuccessful || (DateTime.UtcNow - _lastConnectionAttempt).TotalMinutes >= 5)
        {
            if (success)
            {
                _logger.LogInformation("Successfully connected to OpenTelemetry collector at {Endpoint}. {Message}", 
                    _config.OtelEndpoint, message);
            }
            else
            {
                _logger.LogError("Failed to connect to OpenTelemetry collector at {Endpoint}. {Message}", 
                    _config.OtelEndpoint, message);
            }
            _lastConnectionSuccessful = success;
            _lastConnectionAttempt = DateTime.UtcNow;
        }
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting telemetry service with endpoint: {Endpoint}", _config.OtelEndpoint);

        var tasks = new List<Task>();
        if (_config.EnableLogs)
        {
            tasks.Add(Task.Run(() => EmitLogsAsync(_cancellationTokenSource.Token)));
        }
        if (_config.EnableMetrics)
        {
            tasks.Add(Task.Run(() => EmitMetricsAsync(_cancellationTokenSource.Token)));
        }
        if (_config.EnableTraces)
        {
            tasks.Add(Task.Run(() => EmitTracesAsync(_cancellationTokenSource.Token)));
        }
        await Task.WhenAll(tasks);
    }

    private async Task EmitLogsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("[LOG SIGNAL] Emitting log with label: {Label}", _config.CustomLabel);
                await SendHttpRequestWithHeadersAsync(token, "/v1/logs");
                // Simulate some work
                await Task.Delay(100, token);
                if (!_lastConnectionSuccessful)
                {
                    LogConnectionStatus(true, "Connection recovered (logs)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log emission");
                LogConnectionStatus(false, $"Error (logs): {ex.Message}");
            }
            await Task.Delay(TimeSpan.FromSeconds(_config.IntervalSecondsLogs), token);
        }
    }

    private async Task EmitMetricsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                _operationCounter.Add(1, new KeyValuePair<string, object?>("custom_label", _config.CustomLabel));
                _logger.LogInformation("[METRIC SIGNAL] Emitting metric demo_operations_total with label: {Label}", _config.CustomLabel);
                await SendHttpRequestWithHeadersAsync(token, "/v1/metrics");
                // Simulate some work
                await Task.Delay(100, token);
                if (!_lastConnectionSuccessful)
                {
                    LogConnectionStatus(true, "Connection recovered (metrics)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during metric emission");
                LogConnectionStatus(false, $"Error (metrics): {ex.Message}");
            }
            await Task.Delay(TimeSpan.FromSeconds(_config.IntervalSecondsMetrics), token);
        }
    }

    private async Task EmitTracesAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var activity = _activitySource.StartActivity("demo_operation");
                activity?.SetTag("custom_label", _config.CustomLabel);
                _logger.LogInformation("[TRACE SIGNAL] Emitting trace demo_operation with label: {Label}", _config.CustomLabel);
                await SendHttpRequestWithHeadersAsync(token, "/v1/traces");
                // Simulate some work
                await Task.Delay(100, token);
                activity?.SetStatus(ActivityStatusCode.Ok);
                if (!_lastConnectionSuccessful)
                {
                    LogConnectionStatus(true, "Connection recovered (traces)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during trace emission");
                LogConnectionStatus(false, $"Error (traces): {ex.Message}");
            }
            await Task.Delay(TimeSpan.FromSeconds(_config.IntervalSecondsTraces), token);
        }
    }

    private async Task SendHttpRequestWithHeadersAsync(CancellationToken token, string? path = null)
    {
        if (!_config.ExportProtocol.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            // Only send HTTP request if protocol is 'http'
            return;
        }
        var baseUri = _config.OtelEndpoint.TrimEnd('/');
        var url = path != null ? $"{baseUri}{path}" : baseUri;
        var method = HttpMethod.Post; // OTLP expects POST
        var request = new HttpRequestMessage(method, url);
        if (_config.HttpHeaders != null && _config.HttpHeaders.Count > 0)
        {
            foreach (var kv in _config.HttpHeaders)
            {
                request.Headers.Remove(kv.Key); // Ensure no duplicates
                request.Headers.Add(kv.Key, kv.Value);
            }
        }
        // For demonstration, send empty body (real OTLP would send protobuf payload)
        request.Content = new StringContent(string.Empty);
        try
        {
            _logger.LogInformation("[HTTP] Sending {Method} request to {Url}", method, url);
            var response = await _httpClient.SendAsync(request, token);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[HTTP] Request to {Url} succeeded with status code {StatusCode}", url, response.StatusCode);
            }
            else
            {
                _logger.LogWarning("[HTTP] Request to {Url} failed with status code {StatusCode}", url, response.StatusCode);
            }
            response.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HTTP] Request to {Url} failed with exception", url);
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        LogConnectionStatus(false, "Service stopped");
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        _meter.Dispose();
    }
}