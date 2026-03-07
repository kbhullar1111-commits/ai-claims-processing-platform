# Docker Compose Setup

This folder contains `docker-compose.yml` for local infrastructure required by the claims platform.

## What It Does

The compose file starts two containers:

1. `postgres` (`claims-postgres`)
- Image: `postgres:15`
- Purpose: Main relational database for claims data.
- Environment:
  - `POSTGRES_DB=claimsdb`
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

## Volume

- `postgres-data`: named Docker volume that keeps Postgres data between container restarts.

## How To Run

From this folder:

```bash
docker compose up -d
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
