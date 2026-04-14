
# AI Claims Processing Platform — Architecture Decision Records (ADR)

Date Generated: 2026-03-15

Status Legend:
- Implemented: reflected in the current workspace
- Planned: intended direction, not yet wired in the current workspace

## System Overview
Financial Claims Processing Platform built as an event‑driven microservices architecture using .NET, RabbitMQ, PostgreSQL, and selective MassTransit usage.
Long‑term goal: Azure‑native deployment with AI‑enabled services.

---

# ADR‑001 — Event‑Driven Microservices Architecture
Status: Implemented

Decision:
Services communicate primarily using asynchronous events over RabbitMQ.

Reason:
- Loose coupling
- Horizontal scalability
- Event‑driven workflows

Alternatives Considered:
- Synchronous REST between services
- Shared database

Outcome:
RabbitMQ acts as the event backbone.

Current Implementation Note:
- ClaimsService, NotificationService, and other internal service-to-service flows still use MassTransit over RabbitMQ
- DocumentService now uses a custom RabbitMQ ingress consumer plus a custom database-backed outbox and publisher for document events
- ClaimsService includes a bridge consumer so raw `DocumentUploaded` messages from DocumentService are converted back into the normal typed MassTransit contract before reaching the saga

---

# ADR‑002 — Transactional Outbox Pattern
Status: Implemented

Decision:
Use a transactional outbox pattern for reliable event publication.

Reason:
Guarantee atomicity between database write and event publish.

Problem Solved:
Prevent lost events when DB commit succeeds but message publish fails.

Current Implementation Note:
- ClaimsService uses the MassTransit Entity Framework outbox
- DocumentService uses a custom `OutboxMessages` table, a background dispatcher, and a raw RabbitMQ publisher
- In DocumentService, the `Document` row and outgoing `DocumentUploaded` outbox row are committed in the same database transaction

---

# ADR‑003 — Idempotent Consumers
Status: Implemented

Decision:
NotificationService enforces UNIQUE(EventId, Channel).

Reason:
RabbitMQ provides at‑least‑once delivery; consumers must tolerate duplicates.

Outcome:
Duplicate events will not create duplicate notifications.

---

# ADR‑004 — Database Work Queue Pattern
Status: Implemented

Decision:
Notification processing uses a database‑backed queue (notifications table).

Key Fields:
- Status
- RetryCount
- NextRetryAt

Benefits:
- Reliable background processing
- Retry scheduling
- Failure handling

---

# ADR‑005 — Competing Consumers Using Row Locking
Status: Implemented

Decision:
PostgreSQL query uses FOR UPDATE SKIP LOCKED.

Reason:
Allow multiple dispatcher workers to process notifications safely without duplication.

Outcome:
Supports horizontal scaling when multiple containers run in Kubernetes.

---

# ADR‑006 — Retry with Exponential Backoff
Status: Implemented

Decision:
Retry delay increases exponentially using 2^RetryCount.

Reason:
Prevent hammering downstream systems such as SMTP providers.

---

# ADR‑007 — Notification Strategy Pattern
Status: Implemented

Decision:
Notification delivery implemented via INotificationSender strategy pattern.

Implementations:
- EmailSender
- SmsSender (future)
- PushSender (future)

Benefit:
Extensible notification delivery channels.

---

# ADR‑008 — Structured Logging
Status: Implemented

Decision:
Use Serilog for structured logging.

Current Local Implementation:
- Console logging is enabled
- Rolling file logging is enabled for NotificationService
- Seq remains an optional troubleshooting sink and is enabled through a separate observability compose file plus configuration overrides
- Seq delivery is intentionally non-buffered, so leaving Seq enabled while it is offline does not build up local buffer files

Future Observability:
- Seq for centralized logging
- Azure Monitor in cloud deployment

---

# ADR‑009 — Observability via OpenTelemetry
Status: Implemented

Decision:
Adopt OpenTelemetry for distributed tracing.

Local Development Backend:
Jaeger

Future Cloud Backend:
Azure Application Insights / Azure Monitor

Purpose:
Trace request flows across microservices.

Current Note:
OpenTelemetry is wired in the API startup code across the active services, with OTLP export and Prometheus scraping enabled in local development.

---

# ADR‑010 — Health Checks for Container Environments
Status: Implemented

Decision:
Expose /health, /ready, and /live endpoints using ASP.NET Core HealthChecks.

Reason:
Enable Kubernetes readiness and liveness probes.

Current Implementation:
- `/live` reports process liveness
- `/ready` checks database connectivity
- `/health` exposes the combined health view

---

# ADR‑011 — On‑Demand Database Migrations In Docker Compose
Status: Implemented

Decision:
Keep migrator containers in a separate Docker Compose file instead of the base runtime compose file.

Reason:
- Preserve a reproducible containerized migration workflow
- Avoid coupling normal API startup to one-shot migration jobs
- Keep local startup faster and easier to troubleshoot

Outcome:
- `docker compose up -d` starts the steady-state local stack
- `docker compose -f docker-compose.yml -f docker-compose.migrations.yml up claims-migrator notification-migrator` applies pending migrations when needed

---

# ADR‑012 — Optional Observability Stack For Local Development
Status: Implemented

Decision:
Keep Seq outside the base local compose stack and enable it only through a separate observability compose file.

Reason:
- Normal local startup should not depend on optional tooling
- Services should continue running when Seq is absent
- Observability tooling should be easy to enable when investigation is needed

Outcome:
- Base startup uses `docker compose up -d`
- Seq is enabled with `docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d`
- Both APIs keep Seq disabled by default through configuration, so missing Seq does not break service startup
- Seq shipping is non-buffered by design, so optional observability does not build up local disk buffers when Seq is intentionally offline

---

# Long‑Term Cloud Architecture (Target)
Local Development Stack:
- RabbitMQ
- PostgreSQL
- Docker Compose
- Health endpoints on both APIs
- Optional Seq when troubleshooting is needed

Future Azure Stack:
- Azure Service Bus
- Azure PostgreSQL
- Azure Kubernetes Service (AKS)
- Azure Monitor
- Azure AI Services

---

# Future AI Integration
Planned AI capabilities:
- Document OCR
- Document classification
- Fraud detection models
- Retrieval Augmented Generation (RAG) for policy knowledge
- AI‑assisted claim assessment

Primary AI‑enabled services:
- DocumentService
- FraudService
- AssessmentService
