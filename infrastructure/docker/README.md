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
- Purpose: Run `dotnet ef database update` on demand when schema changes need to be applied.
- These are one-shot containers behind the `migrate` profile and are not part of normal `docker compose up -d`.

4. `claims-api` and `notification-api`
- Start normally without running migrators automatically.
- Expect the database schema to already be up to date.
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

This starts the normal local stack without running migrations.

If you need to apply EF Core migrations:

```bash
docker compose --profile migrate up claims-migrator notification-migrator
```

You can remove the stopped migrator containers afterwards:

```bash
docker compose rm -f claims-migrator notification-migrator
```

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
