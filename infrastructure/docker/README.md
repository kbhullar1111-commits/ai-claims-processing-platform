# Docker Compose Setup

This folder contains local runtime, observability, and migration compose assets for the claims platform.

## Current Compose Files

- `docker-compose.yml`
  - Base runtime stack.
  - Starts `postgres`, `claims-api`, `notification-api`, `document-api`, `fraud-api`, `payment-api`.
  - Uses environment values from `.env` for external connections (Service Bus, Blob Storage, App Insights).

- `docker-compose.observability.yml`
  - Optional overlay.
  - Adds `seq`, `jaeger`, `prometheus`, and `grafana`.
  - Injects OTLP endpoint (`http://jaeger:4317`) and Seq settings into API containers.

- `docker-compose.migrations.yml`
  - Optional migration containers (`claims-migrator`, `notification-migrator`, `document-migrator`).
  - Not used by default startup scripts.

## Important Runtime Notes

- Base runtime compose does not set `OTEL_EXPORTER_OTLP_ENDPOINT`.
- OTLP exporter traffic is expected only when the observability overlay is active or explicit env/config is provided.
- RabbitMQ and MinIO services are currently commented in base compose and are not started by default.

## Commands Folder (Current)

Use the `.cmd` scripts under `commands/`:

- `start.cmd`
  - `docker compose -f docker-compose.yml up -d --build`

- `start-observability.cmd`
  - `docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d --build`

- `stop.cmd`
  - `docker compose -f docker-compose.yml -f docker-compose.observability.yml down %*`

- `migrate.cmd`
  - Runs `dotnet ef database update` for claims, notification, and document contexts against local Postgres.

- `check-outbox.cmd`
  - Executes SQL counts against `documentdb` and `claimsdb` via the running `postgres` container.

## Quick Start

From `infrastructure/docker`:

```bat
commands\start.cmd
```

Apply migrations:

```bat
commands\migrate.cmd
```

Optional observability stack:

```bat
commands\start-observability.cmd
```

Stop everything started by either script:

```bat
commands\stop.cmd
```

Remove volumes too:

```bat
commands\stop.cmd -v
```

## Container Ports

- Postgres: `5432`
- Claims API: `5001`
- Notification API: `5002`
- Document API: `5003`
- Payment API: `5004`
- Fraud API: `5005`

Observability overlay ports:

- Seq: `5341`
- Jaeger UI: `16686`
- Jaeger OTLP gRPC: `4317`
- Jaeger OTLP HTTP: `4318`
- Prometheus: `9090`
- Grafana: `3000`

## Health Endpoints

Each API exposes:

- `/health`
- `/live`
- `/ready`

Examples:

```bash
curl http://localhost:5001/ready
curl http://localhost:5002/ready
curl http://localhost:5003/ready
curl http://localhost:5004/ready
curl http://localhost:5005/ready
```

## Manual Compose Equivalents

Base runtime:

```bash
docker compose -f docker-compose.yml up -d --build
```

Base + observability:

```bash
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d --build
```

Stop base + observability set:

```bash
docker compose -f docker-compose.yml -f docker-compose.observability.yml down
```

Run containerized migrations (alternative to `migrate.cmd`):

```bash
docker compose -f docker-compose.yml -f docker-compose.migrations.yml up claims-migrator notification-migrator document-migrator
```

## Troubleshooting

- If APIs show unhealthy in `docker compose ps`, inspect `/ready` endpoint and container logs.
- If OTLP dependencies to `localhost:4317` appear in Application Insights, verify whether old images are still running and rebuild/recreate containers.
- If environment values are missing, check `infrastructure/docker/.env`.
