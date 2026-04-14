# Workspace Folder Structure

Generated: 2026-04-14

This is a curated view of the current workspace. It highlights solution files,
runtime assets, and architecturally relevant folders. Generated artifacts such as
most `bin/`, `obj/`, and transient `logs/` directories are intentionally omitted.

```text
.
|-- accident-photos.pdf
|-- ai-claims-processing-platform.sln
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
|-- id-proof.pdf
|-- infrastructure/
|   |-- docker/
|   |   |-- docker-compose.migrations.yml
|   |   |-- docker-compose.observability.yml
|   |   |-- docker-compose.yml
|   |   |-- prometheus.yml
|   |   |-- README.md
|   |   |-- commands/
|   |   |   |-- check-outbox.cmd
|   |   |   |-- migrate.cmd
|   |   |   |-- start-observability.cmd
|   |   |   |-- start.cmd
|   |   |   `-- stop.cmd
|   |   `-- initdb/
|   |       `-- 01-create-databases.sql
|   `-- docs/
|       |-- ai_claims_platform_architecture_decisions.md
|       |-- ARCHITECTURE-REVIEW.md
|       |-- Workflow-diagram.md
|       `-- WORKSPACE-STRUCTURE.md
|-- police-report.pdf
|-- services/
|   |-- claims-service/
|   |   |-- ClaimsService.slnx
|   |   |-- ClaimsService.API/
|   |   |   |-- appsettings.Development.json
|   |   |   |-- appsettings.json
|   |   |   |-- ClaimsService.API.csproj
|   |   |   |-- ClaimsService.API.http
|   |   |   |-- Controllers/
|   |   |   |-- Dockerfile
|   |   |   |-- Dockerfile.dockerignore
|   |   |   |-- Program.cs
|   |   |   `-- Properties/
|   |   |-- ClaimsService.Application/
|   |   |   |-- ClaimsService.Application.csproj
|   |   |   |-- Commands/
|   |   |   |-- Handlers/
|   |   |   |-- Interfaces/
|   |   |   `-- Sagas/
|   |   |-- ClaimsService.Domain/
|   |   |   |-- ClaimsService.Domain.csproj
|   |   |   |-- Entities/
|   |   |   `-- Enums/
|   |   `-- ClaimsService.Infrastructure/
|   |       |-- ClaimsService.Infrastructure.csproj
|   |       |-- Messaging/
|   |       |   |-- ClaimStatusConsumer.cs
|   |       |   |-- DocumentUploadedBridgeConsumer.cs
|   |       |   |-- DocumentUploadedRawMessage.cs
|   |       |   `-- EventPublisher.cs
|   |       |-- Observability/
|   |       |-- Persistance/
|   |       `-- Repositories/
|   |-- document-service/
|   |   |-- DocumentService.slnx
|   |   |-- DocumentService.API/
|   |   |   |-- appsettings.Development.json
|   |   |   |-- appsettings.json
|   |   |   |-- Controllers/
|   |   |   |-- DocumentService.API.csproj
|   |   |   |-- DocumentService.API.http
|   |   |   |-- Dockerfile
|   |   |   |-- Dockerfile.dockerignore
|   |   |   |-- Program.cs
|   |   |   |-- Properties/
|   |   |   `-- RequestModels/
|   |   |-- DocumentService.Application/
|   |   |   |-- Commands/
|   |   |   |-- DocumentService.Application.csproj
|   |   |   |-- DTOs/
|   |   |   |-- Interfaces/
|   |   |   `-- Queries/
|   |   |-- DocumentService.Domain/
|   |   |   |-- DocumentService.Domain.csproj
|   |   |   |-- Entities/
|   |   |   |-- Events/
|   |   |   `-- ValueObjects/
|   |   `-- DocumentService.Infrastructure/
|   |       |-- DocumentService.Infrastructure.csproj
|   |       |-- Messaging/
|   |       |   |-- MinioObjectCreated.cs
|   |       |   |-- ObjectCreatedConsumer.cs
|   |       |   |-- OutboxDispatcher.cs
|   |       |   |-- RabbitMqOptions.cs
|   |       |   `-- RabbitPublisher.cs
|   |       |-- Persistence/
|   |       |   |-- DocumentDbContext.cs
|   |       |   `-- InfrastructureEntites/
|   |       |       `-- OutboxMessage.cs
|   |       `-- Storage/
|   |-- fraud-service/
|   |   `-- FraudService.API/
|   |       |-- appsettings.Development.json
|   |       |-- appsettings.json
|   |       |-- Dockerfile
|   |       |-- Dockerfile.dockerignore
|   |       |-- FraudService.API.csproj
|   |       |-- FraudService.API.http
|   |       |-- Program.cs
|   |       |-- Properties/
|   |       `-- RunFraudCheckConsumer.cs
|   |-- notification-service/
|   |   |-- NotificationService.slnx
|   |   |-- NotificationService.API/
|   |   |   |-- appsettings.Development.json
|   |   |   |-- appsettings.json
|   |   |   |-- Dockerfile
|   |   |   |-- Dockerfile.dockerignore
|   |   |   |-- NotificationService.API.csproj
|   |   |   |-- NotificationService.API.http
|   |   |   |-- Program.cs
|   |   |   `-- Properties/
|   |   |-- NotificationService.Application/
|   |   |   |-- Commands/
|   |   |   |-- Interfaces/
|   |   |   `-- NotificationService.Application.csproj
|   |   |-- NotificationService.Domain/
|   |   |   |-- Entities/
|   |   |   |-- Enums/
|   |   |   `-- NotificationService.Domain.csproj
|   |   `-- NotificationService.Infrastructure/
|   |       |-- Messaging/
|   |       |-- NotificationService.Infrastructure.csproj
|   |       |-- Persistence/
|   |       |-- Senders/
|   |       `-- Workers/
|   `-- payment-service/
|       `-- PaymentService.API/
|           |-- appsettings.Development.json
|           |-- appsettings.json
|           |-- Dockerfile
|           |-- Dockerfile.dockerignore
|           |-- PaymentService.API.csproj
|           |-- PaymentService.API.http
|           |-- ProcessPaymentConsumer.cs
|           |-- Program.cs
|           `-- Properties/
`-- Workspace.APIs.http
```