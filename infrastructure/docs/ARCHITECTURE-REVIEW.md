# Architecture Review

Generated: 2026-04-28

## Purpose

This document describes the architecture currently implemented in the workspace and highlights where runtime behavior differs by environment or feature toggle.

## Current Solution Shape

The workspace is organized into service boundaries plus shared contracts:

- `building-blocks/contracts`: shared integration contracts used across services.
- `services/claims-service`: API + saga orchestration for claim lifecycle.
- `services/document-service`: upload URL generation and document persistence/notification path.
- `services/notification-service`: event-driven notification creation and background dispatch.
- `services/fraud-service`: fraud-check consumer endpoint.
- `services/payment-service`: payment-processing consumer endpoint.
- `serverless/document-processor-function`: function-hosted document processor component.
- `infrastructure`: compose files, scripts, and architecture docs.

Core services follow API/Application/Domain/Infrastructure layering.

## Runtime and Integration Summary

### Messaging

- MassTransit is configured with Azure Service Bus in claims, notification, fraud, and payment APIs.
- Claims service includes a document raw-bridge consumer endpoint for document-uploaded events.
- Document service can run a custom raw message/outbox bridge path when MinIO mode is active.

### Storage

- Claims, Notification, and Document services use PostgreSQL with EF Core.
- Document service supports blob storage modes:
	- Azure Blob mode when `ConnectionStrings:BlobStorage` is present.
	- MinIO mode when blob storage connection string is absent.

### Observability

- All APIs use Serilog + Application Insights.
- OpenTelemetry tracing/metrics are enabled across services.
- OTLP exporter is conditional and is only added when endpoint config is provided.
- `/metrics` is exposed for Prometheus scraping.

### Health and Readiness

- All APIs expose `/health`, `/live`, and `/ready`.
- Claims/Notification/Document include DB-backed readiness checks.
- Fraud/Payment currently use self-check readiness.

## Key Flows

### Claim Submission and Saga Flow

1. Request enters claims API controller.
2. Command is sent through MediatR handler.
3. Claim is persisted via repository + unit of work.
4. Integration events are published through MassTransit/Azure Service Bus.
5. Claim processing saga coordinates document, fraud, and payment steps.

### Notification Flow

1. Notification consumer receives claim events from Azure Service Bus.
2. Consumer maps event to application command.
3. Notification row is created idempotently.
4. Background dispatcher pulls pending rows using row locking (`FOR UPDATE SKIP LOCKED`).
5. Sender strategy (email currently) executes delivery and updates status/retry metadata.

### Document Flow

1. Client requests upload URL from document API.
2. Document service returns URL through configured object storage provider.
3. In MinIO mode, object-created events can flow through custom consumer/outbox/publisher.
4. Claims bridge endpoint adapts raw document events into typed workflow events.

## Infrastructure Shape

- Base compose (`docker-compose.yml`) runs postgres + all APIs.
- Observability overlay adds Seq, Jaeger, Prometheus, and Grafana plus OTLP/Seq env overrides.
- Migrations compose exists for containerized migration runs.
- Primary local migration helper script (`commands/migrate.cmd`) runs `dotnet ef` against local postgres.

## Strengths

- Clean separation of application and infrastructure concerns.
- Explicit workflow orchestration with saga state machine.
- Outbox patterns in key reliability boundaries.
- Observability stack can be enabled on demand without changing code.
- Consistent health endpoints and container readiness checks.

## Gaps and Risks

- Mixed integration modes in document path (Azure Blob mode vs MinIO bridge mode) increase operational complexity.
- Claims infrastructure folder naming still uses `Persistance` spelling.
- OTLP/AI dual instrumentation can create noisy dependency telemetry when optional collectors are enabled or misconfigured.
- Notification sender remains stub-level for real provider integration hardening.

## Maintenance Guidance

Update this file when:

- message transport wiring changes
- saga transitions or contracts change
- storage mode defaults change
- health/readiness semantics change
- observability exporter behavior changes