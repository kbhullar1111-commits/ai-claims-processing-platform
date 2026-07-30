# Architecture Review

Updated: 2026-07-30

## Purpose

This document summarizes the current architecture of the repository and captures the direction of the implementation as it stands today.

## Current Solution Shape

The workspace is organized around multiple service boundaries plus shared contracts and infrastructure assets:

- building-blocks/contracts: shared contracts used by the services for integration events and API payloads.
- services/claims-service: claim lifecycle API and workflow orchestration entry point.
- services/document-service: document upload, persistence, and event-driven processing integration.
- services/notification-service: notification creation and background dispatch.
- services/fraud-service: fraud-check processing endpoint.
- services/payment-service: payment-processing endpoint.
- services/gateway-service: gateway entry point for routing and aggregation.
- services/customer-service: customer-related domain capabilities.
- serverless/document-processor-function: Azure Functions host for document processing scenarios.
- platform/deployment: deployment assets and console tooling.
- infrastructure: Docker compose runtime, migration assets, and documentation.

The application services follow a layered structure with API, Application, Domain, and Infrastructure responsibilities.

## Cloud Infrastructure (Azure)

### Current Azure Resources

**Compute & Container Registry**
- Azure Container Registry (ACR): Available for image storage (not yet actively used for CI/CD)
- Azure Container Apps Environment
  - Workload Profile: Consumption (scale-to-zero capable)
  - Region: Central India

**Data & Storage**
- Azure PostgreSQL Flexible Server
  - Name: `pg-ai-claims-dev`
  - Version: PostgreSQL 16
  - Tier: Burstable (B1ms)
  - SSL/TLS: Required (enforced)
  - Databases: `claimsdb`, `notificationdb`, `documentdb`

- Azure Blob Storage
  - Account: `staiclaimsdev01`
  - Used for document uploads and serverless function triggers

**Messaging & Integration**
- Azure Service Bus
  - Namespace: `sb-aiclaims-dev-kamal01`
  - Primary transport for async messaging between services

**Secrets & Configuration**
- Azure Key Vault
  - Name: `kv-aiclaims-dev`
  - Status: Configured but not fully integrated (local development uses direct config)

**Observability**
- Application Insights
  - Instrumentation Key: `1e5a9a7d-fc4f-41eb-a2ae-4d79562a6983`
  - Region: Central India

## Deployment Model

### Current Deployment Strategy

**Local Development**
- Docker Compose orchestrates all services locally
- PostgreSQL, RabbitMQ/Service Bus connectivity configured in docker-compose.yml
- Services connect to Azure PostgreSQL and Service Bus directly (hybrid model)

**Cloud Deployment**
- Partial: Services are containerized with multi-stage Dockerfiles
- Ready for Azure Container Apps deployment
- ACR integration available but CI/CD pipeline not yet implemented

### Containerization Approach

- Multi-stage Dockerfiles optimize image size
- Images built locally and pushed to ACR (manual process)
- Azure Container Apps pull images on-demand
- Health probes configured for startup, liveness, and readiness

### Planned Evolution

- GitHub Actions CI/CD pipeline for automated builds/deploys
- Container image scanning and vulnerability assessment
- Infrastructure-as-Code (Bicep/ARM templates or Terraform)
- AKS migration path for advanced orchestration scenarios
- Managed Identity authentication for service-to-service calls

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

## Security Posture

### Current State

**Data in Transit**
- PostgreSQL: SSL/TLS enforced on Azure Flexible Server
- Service Bus: Encrypted by default (HTTPS)
- Service-to-service: HTTP (local) and HTTPS (Azure Container Apps)

**Secrets Management**
- Development: Secrets stored in `appsettings.Development.json` (local file, not committed)
- Production-ready path: Azure Key Vault (configured but not yet enforced)
- Connection strings and API keys currently in configuration; no environment-based secret rotation

**Network Access**
- Azure PostgreSQL: Firewall rules restrict direct access
- Service Bus: Shared access keys used for authentication
- Container Apps: Public endpoints (no private networking yet)

### Planned Security Enhancements

- Azure Key Vault integration with automatic rotation
- Managed Identities for service-to-service authentication (eliminate connection string secrets)
- Private VNet integration for Azure resources
- Network Security Groups (NSGs) and private endpoints
- Secret scanning in CI/CD pipeline
- API rate limiting and DDoS protection

## Reliability Patterns

### Currently Implemented

- **Saga Orchestration**: Claim processing coordinated across document, fraud, and payment services
- **Transactional Outbox**: MassTransit outbox ensures events are published atomically with state changes
- **Inbox Deduplication**: Prevents duplicate processing of repeated messages
- **Health & Readiness Probes**: All services expose `/health`, `/live`, and `/ready` endpoints
  - Claims/Notification/Document services include database connectivity checks
  - Fraud/Payment use self-check readiness
- **Retry Handling**: MassTransit configured for automatic exponential backoff
- **Idempotent Processing**: Notification consumer processes events idempotently
- **Row-Level Locking**: Notification background dispatcher uses `FOR UPDATE SKIP LOCKED` to prevent duplicate sends

### Future Reliability Goals

- Dead-letter queue (DLQ) strategy for failed messages
- Circuit breaker patterns for external service calls
- Distributed caching layer (Redis) for performance and resilience
- Multi-region failover and geo-redundancy
- Chaos engineering and chaos testing
- Enhanced monitoring of saga state transitions

## Cost Optimization Strategy

### Current Cost-Control Practices

**Infrastructure**
- Azure Container Apps: Consumption plan (pay per execution)
- PostgreSQL: Burstable tier (B1ms) for non-production learning phase
- Service Bus: Shared namespace across all services
- Blob Storage: Standard tier with lifecycle policies possible

**Operational**
- Manual stop/start of PostgreSQL server when not in use
- Shared database server across services during learning phase
- No scheduled auto-shutdown for Container Apps (running 24/7)

### Planned Cost Optimization

- Automated non-production environment shutdown (evenings/weekends)
- Infrastructure automation scripts for resource tear-down
- Ephemeral review environments for feature branches (short-lived)
- Azure Reservations for predictable workloads
- Container image layer caching to reduce build times and ACR storage
- PostgreSQL connection pooling to reduce database connections
- Application Insights sampling rules to reduce data ingestion costs

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