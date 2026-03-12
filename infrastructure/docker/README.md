# Docker Compose Setup

This folder contains `docker-compose.yml` for local infrastructure and service runtime required by the claims platform.

## What It Does

The compose file starts these containers:

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

3. `claims-migrator` and `notification-migrator`
- Image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Purpose: Run `dotnet ef database update` before APIs start.
- These are one-shot containers and must complete successfully.

4. `claims-api` and `notification-api`
- Start only after their corresponding migrator container completes.
- Use RabbitMQ for publish/consume event flow.
- Port mappings:
  - Claims API: `5001:8080`
  - Notification API: `5002:8080`

## Volume

- `postgres-data`: named Docker volume that keeps Postgres data between container restarts.

## How To Run

From this folder:

```bash
docker compose up -d
```

This automatically runs migrations for both services before starting API containers.

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
