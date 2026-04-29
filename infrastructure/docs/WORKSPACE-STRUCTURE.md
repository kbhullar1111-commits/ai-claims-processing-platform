# Workspace Folder Structure

Generated: 2026-04-28

Curated structure view based on current code and runtime assets.
Generated folders (`bin`, `obj`, publish output) are intentionally omitted.

```text
.
|-- ai-claims-processing-platform.sln
|-- Workspace.APIs.http
|-- building-blocks/
|   |-- contracts/
|   |   `-- BuildingBlocks.Contracts/
|   |       |-- BuildingBlocks.Contracts.csproj
|   |       |-- Claims/
|   |       |-- Documents/
|   |       |-- Fraud/
|   |       |-- Payment/
|   |       `-- Payments/
|   `-- messaging/
|-- infrastructure/
|   |-- docker/
|   |   |-- .env
|   |   |-- README.md
|   |   |-- docker-compose.yml
|   |   |-- docker-compose.migrations.yml
|   |   |-- docker-compose.observability.yml
|   |   |-- prometheus.yml
|   |   |-- commands/
|   |   |   |-- check-outbox.cmd
|   |   |   |-- migrate.cmd
|   |   |   |-- start.cmd
|   |   |   |-- start-observability.cmd
|   |   |   `-- stop.cmd
|   |   `-- initdb/
|   |       `-- 01-create-databases.sql
|   `-- docs/
|       |-- ai_claims_platform_architecture_decisions.md
|       |-- ARCHITECTURE-REVIEW.md
|       |-- OBSERVABILITY-QUERIES.md
|       |-- Workflow-diagram.md
|       `-- WORKSPACE-STRUCTURE.md
|-- services/
|   |-- claims-service/
|   |   |-- ClaimsService.slnx
|   |   |-- ClaimsService.API/
|   |   |-- ClaimsService.Application/
|   |   |-- ClaimsService.Domain/
|   |   `-- ClaimsService.Infrastructure/
|   |-- document-service/
|   |   |-- DocumentService.slnx
|   |   |-- DocumentService.API/
|   |   |-- DocumentService.Application/
|   |   |-- DocumentService.Domain/
|   |   `-- DocumentService.Infrastructure/
|   |-- notification-service/
|   |   |-- NotificationService.slnx
|   |   |-- NotificationService.API/
|   |   |-- NotificationService.Application/
|   |   |-- NotificationService.Domain/
|   |   `-- NotificationService.Infrastructure/
|   |-- fraud-service/
|   |   `-- FraudService.API/
|   `-- payment-service/
|       `-- PaymentService.API/
`-- serverless/
	`-- document-processor-function/
```

## Notes

- Local runtime is compose-based under `infrastructure/docker`.
- Base compose currently runs postgres plus all API containers.
- Observability stack is opt-in via `docker-compose.observability.yml`.