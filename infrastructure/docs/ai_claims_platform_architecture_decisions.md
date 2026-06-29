
# AI Claims Processing Platform - Architecture Decision Records (ADR)

Date Generated: 2026-04-28

Status Legend:
- Implemented: reflected in current workspace code
- Planned: intended direction, not yet fully wired

## System Overview
Financial claims platform built with .NET microservices, PostgreSQL, Azure Service Bus, and OpenTelemetry/Application Insights.

The local compose stack now runs only steady-state runtime dependencies in the base file. Optional observability tooling (Seq, Jaeger, Prometheus, Grafana) is enabled via overlay compose.

---

# ADR-001 - Event-Driven Microservices Architecture
Status: Implemented

Decision:
Services communicate asynchronously through integration events.

Current Implementation Note:
- Claims, Notification, Fraud, and Payment services use MassTransit on Azure Service Bus.
- Document service uses HTTP plus storage integration and can run an internal raw RabbitMQ-style bridge path only when MinIO mode is enabled.

Reason:
- Loose coupling
- Independent service scaling
- Resilient workflow progression

---

# ADR-002 - Transactional Outbox Pattern
Status: Implemented

Decision:
Use transactional outbox for reliable event publication where needed.

Current Implementation Note:
- Claims service uses MassTransit EF outbox (`UseBusOutbox`).
- Document service persists document + outbox in one transaction for its custom publisher path.

Reason:
Prevents lost messages when DB commit and broker publish happen at different times.

---

# ADR-003 - Idempotent Notification Creation
Status: Implemented

Decision:
Notification processing keeps idempotency safeguards so duplicate events do not create duplicate notifications.

Reason:
At-least-once delivery semantics require duplicate-safe consumers.

---

# ADR-004 - Database Work Queue Pattern
Status: Implemented

Decision:
Notification dispatch uses a DB-backed queue with lifecycle state.

Queue State Fields:
- Status
- RetryCount
- NextRetryAt

Benefits:
- Reliable background processing
- Controlled retries
- Operational visibility

---

# ADR-005 - Competing Consumers With Row Locking
Status: Implemented

Decision:
Notification dispatcher acquires work via `FOR UPDATE SKIP LOCKED`.

Reason:
Allows multiple workers to process pending notifications safely without double handling.

---

# ADR-006 - Retry With Exponential Backoff
Status: Implemented

Decision:
Notification retry scheduling increases delay as attempts grow.

Reason:
Reduces load on external providers during failure windows.

---

# ADR-007 - Notification Sender Strategy
Status: Implemented

Decision:
Use `INotificationSender` strategy abstraction.

Current Implementation:
- Email sender is active
- SMS and push are extension points

---

# ADR-008 - Structured Logging and AI Telemetry
Status: Implemented

Decision:
Use Serilog with Application Insights sink and console logging across APIs.

Current Implementation Note:
- Notification service also writes rolling local files.
- Optional Seq sink is enabled only through overlay configuration.

---

# ADR-009 - OpenTelemetry With Conditional OTLP Export
Status: Implemented

Decision:
OpenTelemetry tracing/metrics are enabled across services, but OTLP exporter is added only when endpoint configuration is provided.

Runtime Behavior:
- `OTEL_EXPORTER_OTLP_ENDPOINT` (or `Observability:Otlp:Endpoint`) controls whether exporter is wired.
- Base compose does not set this endpoint.
- Observability overlay sets OTLP endpoint to Jaeger.

Reason:
Avoid unintended default OTLP traffic when optional observability stack is not running.

---

# ADR-010 - Health Probes for Containerized Services
Status: Implemented

Decision:
Expose `/health`, `/live`, and `/ready` endpoints.

Current Implementation:
- Claims/Notification/Document: readiness checks include Postgres reachability.
- Fraud/Payment: current readiness is self-check based.

---

# ADR-011 - Migrations Kept Separate From Normal Runtime Startup
Status: Implemented

Decision:
Run migrations outside base runtime startup.

Current Workflow:
- Base stack startup remains focused on API/runtime containers.
- Separate migration compose file exists.
- Primary helper script currently runs `dotnet ef database update` locally for claims, notification, and document DBs.

---

# ADR-012 - Optional Observability Overlay
Status: Implemented

Decision:
Use `docker-compose.observability.yml` overlay for local observability stack.

Included Components:
- Seq
- Jaeger (OTLP collector)
- Prometheus
- Grafana

Outcome:
- Base startup remains minimal.
- Deep diagnostics can be enabled on demand without changing app code.

---

# Long-Term Cloud Direction
Target stack remains Azure-native:
- Azure Service Bus
- Azure Database for PostgreSQL
- Azure Monitor / Application Insights
- AKS or managed container hosting
- AI-enabled claim workflows

---

# Future AI Integration
Planned capabilities:
- OCR and document extraction
- Document classification
- Fraud scoring
- Retrieval-augmented policy guidance
- Assisted claim adjudication
