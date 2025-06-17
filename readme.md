# Dynatrace in Azure Container Apps

## Overview

This note provides guidance on deploying Dynatrace in Azure Container Apps and testing telemetry data ingestion using `telemetrygen` and `otel-cli`.

---

### Prerequisites

- [Telemetrygen](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/cmd/telemetrygen)
- [otel-cli](https://github.com/equinix-labs/otel-cli)
- [Dynatrace API Token](https://dt-url.net/DTAPIToken)
- [Dynatrace Environment URL](https://dt-url.net/DTEndpoint)

---

### Sending Telemetry Data to Dynatrace

#### Using Telemetrygen

Generate metrics, logs, and traces for testing:

```sh
export DT_TOKEN=your_dynatrace_api_token_here
export DT_ENDPOINT=shw95809.live.dynatrace.com

telemetrygen metrics --otlp-http --otlp-endpoint $DT_ENDPOINT:443 --otlp-http-url-path "/api/v2/otlp/v1/metrics" --otlp-header "authorization=Api-Token $DT_TOKEN" --duration 600s

telemetrygen logs --otlp-http --otlp-endpoint $DT_ENDPOINT:443 --otlp-http-url-path "/api/v2/otlp/v1/logs" --otlp-header "authorization=Api-Token $DT_TOKEN" --duration 600s

telemetrygen traces --otlp-http --otlp-endpoint $DT_ENDPOINT:443 --otlp-http-url-path "/api/v2/otlp/v1/traces" --otlp-header "authorization=Api-Token $DT_TOKEN" --duration 600s
```

Check the generated data in Dynatrace.

#### Using otel-cli

Send spans to Dynatrace:

```sh
export DT_TOKEN=your_dynatrace_api_token_here
export DT_ENDPOINT=shw95809.live.dynatrace.com
for run in {1..10000}; do otel-cli span \
  --endpoint "https://$DT_ENDPOINT/api/v2/otlp/v1/traces" \
  --otlp-headers "Authorization=Api-Token ${DT_TOKEN}" \
  --protocol http/protobuf \
  --verbose; done
```

---

### Deploying Azure Container Apps with Dynatrace Telemetry

Deploy an Azure Container App environment and configure OTLP endpoints for Dynatrace:

```sh
RG=aca
LOCATION=swedencentral
ENVIRONMENT_NAME=aca-env-dt
OTLP_NAME=otlp-dt
ENDPOINT_URL="https://shw95809.live.dynatrace.com/api/v2/otlp/"

az containerapp env create \
  --name $ENVIRONMENT_NAME \
  --resource-group $RG \
  --location $LOCATION

az containerapp env telemetry otlp add \
  --resource-group $RG \
  --name $ENVIRONMENT_NAME \
  --otlp-name ${OTLP_NAME}-metrics \
  --endpoint ${ENDPOINT_URL}/v1/metrics \
  --insecure false \
  --headers "Authorization=Api-Token ${DT_TOKEN}" \
  --enable-open-telemetry-traces false \
  --enable-open-telemetry-metrics true \
  --enable-open-telemetry-logs false

az containerapp env telemetry otlp add \
  --resource-group $RG \
  --name $ENVIRONMENT_NAME \
  --otlp-name ${OTLP_NAME}-traces \
  --endpoint ${ENDPOINT_URL}/v1/traces \
  --insecure false \
  --headers "Authorization=Api-Token ${DT_TOKEN}" \
  --enable-open-telemetry-traces true \
  --enable-open-telemetry-metrics false \
  --enable-open-telemetry-logs false

az containerapp env telemetry otlp add \
  --resource-group $RG \
  --name $ENVIRONMENT_NAME \
  --otlp-name ${OTLP_NAME}-logs \
  --endpoint ${ENDPOINT_URL}/v1/logs \
  --insecure false \
  --headers "Authorization=Api-Token ${DT_TOKEN}" \
  --enable-open-telemetry-traces false \
  --enable-open-telemetry-metrics false \
  --enable-open-telemetry-logs true
```

---

> **Note:**  
> The Azure Container Apps OpenTelemetry Collector (in preview) does **not** support the OTLP HTTP protocol required by Dynatrace. To send telemetry data to Dynatrace, use a sidecar container running the OpenTelemetry Collector configured for OTLP HTTP.

---

![Dynatrace Example](image.png)