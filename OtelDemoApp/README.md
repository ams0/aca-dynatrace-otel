# OpenTelemetry Demo App

A simple .NET application that demonstrates OpenTelemetry integration by generating logs, metrics, and traces at configurable intervals.

## Features

- Generates logs, metrics, and traces using OpenTelemetry
- Configurable via environment variables
- Uses OTLP exporter to send telemetry data
- Includes custom labels and intervals
- **Enable/disable logs, metrics, traces via environment variables**
- **Separate interval per type of signal (logs, metrics, traces)**
- **Send custom HTTP headers on each request via OTEL_HTTP_HEADERS (only if OTEL_EXPORT_PROTOCOL is 'http')**

## Configuration

The application can be configured using the following environment variables:

- `OTEL_ENDPOINT`: The OpenTelemetry endpoint (default: http://localhost:4317)
- `OTEL_EXPORT_PROTOCOL`: Export protocol, either `protobuf` (default) or `http`. If set to `http`, custom headers can be sent.
- `OTEL_INTERVAL_SECONDS`: Default interval between telemetry generations in seconds (default: 5)
- `OTEL_INTERVAL_SECONDS_LOGS`: Interval for emitting logs (overrides `OTEL_INTERVAL_SECONDS` for logs)
- `OTEL_INTERVAL_SECONDS_METRICS`: Interval for emitting metrics (overrides `OTEL_INTERVAL_SECONDS` for metrics)
- `OTEL_INTERVAL_SECONDS_TRACES`: Interval for emitting traces (overrides `OTEL_INTERVAL_SECONDS` for traces)
- `OTEL_ENABLE_LOGS`: Enable log emission (`true`/`false`, default: `true`)
- `OTEL_ENABLE_METRICS`: Enable metrics emission (`true`/`false`, default: `true`)
- `OTEL_ENABLE_TRACES`: Enable trace emission (`true`/`false`, default: `true`)
- `OTEL_CUSTOM_LABEL`: Custom label to be added to telemetry data (default: demo-app)
- `OTEL_HTTP_HEADERS`: Comma-separated list of HTTP headers to send on each request (only if `OTEL_EXPORT_PROTOCOL` is `http`), e.g. `Authorization=Bearer token,Custom-Header=Value`

## Building and Running

1. Build the application:

```bash
dotnet build
```

2. Run the application:

```bash
dotnet run
```

Or with custom configuration:

```bash
OTEL_ENDPOINT=http://your-otel-endpoint:4317 \
OTEL_EXPORT_PROTOCOL=http \
OTEL_INTERVAL_SECONDS=10 \
OTEL_INTERVAL_SECONDS_LOGS=5 \
OTEL_INTERVAL_SECONDS_METRICS=15 \
OTEL_INTERVAL_SECONDS_TRACES=20 \
OTEL_ENABLE_LOGS=true \
OTEL_ENABLE_METRICS=false \
OTEL_ENABLE_TRACES=true \
OTEL_CUSTOM_LABEL=my-label \
OTEL_HTTP_HEADERS="Authorization=Bearer token,Custom-Header=Value" \
dotnet run
```

## Telemetry Data

The application generates the following telemetry data:

### Logs
- Application startup and shutdown logs
- Operation processing logs with custom labels
- Error logs when exceptions occur

### Metrics
- Counter: `demo_operations_total` - Total number of operations performed
- Includes custom label as a dimension

### Traces
- Activity: `demo_operation` - Represents each operation
- Includes custom label as a tag
- Includes operation status (OK/Error)

## Dependencies

- .NET 8.0
- OpenTelemetry packages
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging