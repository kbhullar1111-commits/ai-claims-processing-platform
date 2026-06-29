# ACA Config Troubleshooting Runbook

This runbook is for production issues such as missing Key Vault values, broken startup configuration, and unexpected Service Bus topology behavior.

## 1) Quick Triage

1. Confirm active revisions and replicas:

```bash
az containerapp revision list --name <app-name> --resource-group <rg> -o table
```

2. Pull recent logs (startup and runtime):

```bash
az containerapp logs show --name <app-name> --resource-group <rg> --tail 300
```

3. Inspect effective runtime env values:

```bash
az containerapp show --name <app-name> --resource-group <rg> --query "properties.template.containers[0].env" -o table
```

## 2) Key Vault Checks

1. Verify app has managed identity enabled:

```bash
az containerapp show --name <app-name> --resource-group <rg> --query "identity" -o json
```

2. Verify Key Vault secret names match .NET configuration mapping:
- `ConnectionStrings:ServiceBus` -> `ConnectionStrings--ServiceBus`
- `ConnectionStrings:ClaimsPostgres` -> `ConnectionStrings--ClaimsPostgres`
- `ConnectionStrings:NotificationPostgres` -> `ConnectionStrings--NotificationPostgres`
- `ConnectionStrings:BlobStorage` -> `ConnectionStrings--BlobStorage`
- `ApplicationInsights:ConnectionString` -> `ApplicationInsights--ConnectionString`
- `KeyVault:Url` -> `KeyVault--Url`

3. Verify the app can read Key Vault values in production mode:
- Ensure `KeyVault:Url` is present in `appsettings.json` (not just `appsettings.Development.json`).
- Or set `KeyVault__Url` as an ACA env var.

## 3) Service Bus Topology Checks

1. List queues:

```bash
az servicebus queue list --resource-group <rg> --namespace-name <sb-namespace> -o table
```

2. List topics:

```bash
az servicebus topic list --resource-group <rg> --namespace-name <sb-namespace> -o table
```

3. List subscriptions for an event topic:

```bash
az servicebus topic subscription list \
  --resource-group <rg> \
  --namespace-name <sb-namespace> \
  --topic-name <event-topic-name> \
  -o table
```

If a queue keeps growing unexpectedly, check whether a stale subscription forwards to it.

## 4) Common Symptoms and Causes

1. `Missing ... connection string` during startup:
- Required config key missing.
- Key Vault provider not loaded because `KeyVault:Url` missing in production config.

2. `The ConnectionString property has not been initialized`:
- Wrong connection string key name in code.
- Value absent in runtime config.

3. One queue grows but business flow still works:
- Stale topic subscription forwarding to an orphan queue.

## 5) Safe Cleanup of Stale Service Bus Topology

1. Confirm active subscriptions for the topic.
2. Delete only stale subscription (not active consumers).
3. Re-run one business flow and verify expected queues move.
4. Delete orphan queue only after validation.

## 6) Recommended Startup Guardrails

Each API should fail fast with clear errors for required keys:
- `ApplicationInsights:ConnectionString`
- `ConnectionStrings:ServiceBus`
- Service-specific DB/Storage connection strings

For DB-backed services, run a startup connectivity check with timeout to avoid endless background retry logs.

## 7) Release Validation Checklist

1. Build image from latest code.
2. Deploy and verify latest revision is active.
3. Confirm startup logs have no configuration errors.
4. Execute one end-to-end flow.
5. Verify DB writes and queue/topic movement.
6. Verify no orphan queue growth.
