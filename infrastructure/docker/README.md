# Docker Compose Setup

This folder contains the local Docker Compose files used by the claims platform.

## What It Does

The base compose file starts these steady-state runtime containers:

1. `postgres` (`claims-postgres`)
- Image: `postgres:15`
- Purpose: Main relational database server that hosts `claimsdb` and `notificationdb`.
- Environment:
  - `POSTGRES_DB=postgres`
  - `POSTGRES_USER=postgres`
  - `POSTGRES_PASSWORD=postgres`
- Port mapping: `5432:5432`
- Persistent storage: `postgres-data` volume mounted at `/var/lib/postgresql/data`

2. `rabbitmq` (`claims-rabbitmq`)
- Image: `rabbitmq:3-management`
- Purpose: Message broker for asynchronous events between services.
- Port mappings:
  - `5672:5672` for AMQP messaging
  - `15672:15672` for RabbitMQ management UI

3. `claims-api` and `notification-api`
- Start normally without running migrators automatically.
- Expect the database schema to already be up to date.
- Use RabbitMQ for publish/consume event flow.
- Both services wait for `postgres` to pass its healthcheck (`pg_isready`) before starting.
- Both expose `/live` (liveness) and `/ready` (readiness — checks Postgres connectivity) endpoints intended for orchestrator probes, not public access.
- Docker tracks each API's health via `/ready`; use `docker compose ps` to see health status.
- Port mappings:
  - Claims API: `5001:8080`
  - Notification API: `5002:8080`

4. `docker-compose.observability.yml`
- Purpose: optional local observability stack and related API overrides.
- Current use: starts Seq and enables Seq sinks in both APIs only when explicitly requested.

5. `docker-compose.migrations.yml`
- Purpose: one-shot EF Core migrator containers for schema updates.
- Current use: runs claims and notification migrations on demand without placing migrator jobs in the normal runtime stack.

## Volume

- `postgres-data`: named Docker volume that keeps Postgres data between container restarts.

## Network

All services (including observability and migration containers) join a single explicitly declared bridge network: `claims-network`.

This prevents the common problem of partial teardown leaving orphaned containers attached to Docker's default network. Because the network is declared in the base compose file and marked `external: true` in the overlays, Docker manages it predictably.

To inspect the network:
```bash
docker network inspect claims-network
```

## How To Run

PowerShell scripts are in the `commands/` subfolder. Run them from anywhere — they resolve the compose file paths automatically.

### One-time setup (new machine)
```powershell
.\commands\setup.ps1
```

### Start base stack
```powershell
.\commands\start.ps1
```

### Start with observability (Seq + Jaeger)
```powershell
.\commands\start-observability.ps1
```

### Run database migrations
Run this after first start or after schema changes:
```powershell
.\commands\migrate.ps1
```

### Stop all containers (keep DB data)
```powershell
.\commands\stop.ps1
```

### Stop all containers and wipe DB data
```powershell
.\commands\stop.ps1 -v
```

> **Important:** Always use `commands\stop.ps1` to shut down. It always includes the observability overlay so all containers (including seq and jaeger) are properly removed and the `claims-network` is cleaned up. This avoids the orphaned network problem that occurs when shutting down with a different compose file set than was used to start.

If you need to apply EF Core migrations:

```bash
docker compose -f docker-compose.yml -f docker-compose.migrations.yml up claims-migrator
docker compose -f docker-compose.yml -f docker-compose.migrations.yml up notification-migrator
```

You can remove the stopped migrator containers afterwards:

```bash
docker compose rm -f claims-migrator notification-migrator
```

This keeps migration jobs completely out of the base compose file.

To stop:

```bash
docker compose down
```

To stop and remove Postgres data as well:

```bash
docker compose down -v
```

## Access

- Postgres host: `localhost`, port: `5432`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ UI: `http://localhost:15672`
- Claims API: `http://localhost:5001`
- Notification API: `http://localhost:5002`
- Seq when enabled: `http://localhost:5341` (internal port 8080, mapped to host 5341)

### Health Check Endpoints

These are infrastructure probes intended for orchestrators (Kubernetes liveness/readiness probes). They are not part of the public API and are not exposed in Swagger.

| Endpoint | Purpose | Expected response |
|---|---|---|
| `GET /health` | All checks combined | `200 Healthy` |
| `GET /live` | Liveness — is the process running? | Always `200 Healthy` |
| `GET /ready` | Readiness — is Postgres reachable? | `200 Healthy` or `503 Unhealthy` |

Claims API:
```bash
curl http://localhost:5001/health
curl http://localhost:5001/live
curl http://localhost:5001/ready
```

Notification API:
```bash
curl http://localhost:5002/health
curl http://localhost:5002/live
curl http://localhost:5002/ready
```

### Swagger UI

Available in all environments for local development convenience:

- Claims API: `http://localhost:5001/swagger`
- Notification API: `http://localhost:5002/swagger`

## Useful Commands

Run these from this folder.

1. `docker compose up -d`
Starts the normal local runtime stack in the background.

2. `docker compose up --build -d`
Builds the API images if needed and then starts the normal local runtime stack.

3. `docker compose down`
Stops and removes the current runtime containers and network.

4. `docker compose down -v`
Stops and removes the current runtime containers and also deletes named volumes such as the Postgres data volume.

5. `docker compose ps`
Shows the status of the runtime containers.

6. `docker compose logs -f`
Streams logs from all services in the base runtime stack.

7. `docker compose logs -f claims-api`
Streams only claims API logs.

8. `docker compose logs -f notification-api`
Streams only notification API logs.

9. `docker compose build claims-api`
Builds only the claims API image.

10. `docker compose build notification-api`
Builds only the notification API image.

11. `docker compose restart claims-api`
Restarts only the claims API container.

12. `docker compose restart notification-api`
Restarts only the notification API container.

13. `docker compose exec claims-api /bin/sh`
Opens a shell inside the running claims API container.

14. `docker compose exec notification-api /bin/sh`
Opens a shell inside the running notification API container.

15. `docker compose config`
Prints and validates the effective base compose configuration without starting containers.

16. `docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d`
Starts the runtime stack plus Seq and enables Seq logging in both APIs.

17. `docker compose -f docker-compose.yml -f docker-compose.observability.yml logs -f seq`
Streams Seq logs when the observability overlay is running.

18. `docker compose -f docker-compose.yml -f docker-compose.observability.yml config`
Prints and validates the merged runtime plus observability configuration.

19. `docker compose -f docker-compose.yml -f docker-compose.migrations.yml up claims-migrator`
Runs the claims service migration container on demand.

20. `docker compose -f docker-compose.yml -f docker-compose.migrations.yml up notification-migrator`
Runs the notification service migration container on demand.

21. `docker compose rm -f claims-migrator notification-migrator`
Removes the stopped migration containers after they finish.

22. `docker compose -f docker-compose.yml -f docker-compose.migrations.yml config`
Prints and validates the merged runtime plus migrations configuration.

23. `commands\migrate.cmd`
Runs claims and notification migrations sequentially using the standard compose overlays.
