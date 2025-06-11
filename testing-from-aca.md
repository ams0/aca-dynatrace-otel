# Testing from ACA

Use the nicolaka/netshoot image (using `-c, while true; do sleep 1; done` to keep the container running) to test connectivity from the Container App to the Dynatrace endpoint.

```bash
az containerapp create \
  --name test-aca \
  --resource-group $RG \
  --environment $ENVIRONMENT_NAME \
  --image nicolaka/netshoot \
  --cpu 0.5 \
  --memory 1.0Gi \
  --command "while true; do sleep 1; done" \
  --target-port 80
```

Then, use the `az containerapp exec` command to run commands inside the container:

```bash
az containerapp exec \
  --name test-aca \
  --resource-group $RG \
  --exec-command "curl -v -H 'Authorization: Api-Token ${DT_TOKEN}' ${ENDPOINT_URL}/v1/metrics"
```