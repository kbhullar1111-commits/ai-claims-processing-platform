# Workspace Folder Structure

Generated: 2026-04-07

This is a detailed, curated view of the workspace. It focuses on solution structure,
runtime assets, and implementation folders that matter architecturally. Generated
artifacts such as most `bin/` and `obj/` directories are intentionally omitted.

```text
.
|-- ai-claims-processing-platform.sln
|-- building-blocks/
|   |-- contracts/
|   |   `-- BuildingBlocks.Contracts/
|   |       |-- BuildingBlocks.Contracts.csproj
|   |       |-- Claims/
|   |       |   `-- ClaimSubmitted.cs
|   |       |-- Documents/
|   |       |   `-- DocumentsUploaded.cs
|   |       |-- Fraud/
|   |       |   `-- FraudCheckCompleted.cs
|   |       `-- Payments/
|   |           `-- PaymentCompleted.cs
|   `-- messaging/
|-- infrastructure/
|   |-- docker/
|   |   |-- docker-compose.yml
|   |   |-- docker-compose.migrations.yml
|   |   |-- docker-compose.observability.yml
|   |   |-- commands/
|   |   |   |-- migrate.cmd
|   |   |   |-- start-observability.cmd
|   |   |   |-- start.cmd
|   |   |   `-- stop.cmd
|   |   |-- initdb/
|   |   |   `-- 01-create-databases.sql
|   |   |-- prometheus.yml
|   |   `-- README.md
|   `-- docs/
|       |-- ai_claims_platform_architecture_decisions.md
|       |-- ARCHITECTURE-REVIEW.md
|       |-- WORKSPACE-STRUCTURE.md
|       `-- Workflow-diagram.md
`-- services/
    |-- claims-service/
    |   |-- ClaimsService.slnx
    |   |-- ClaimsService.API/
    |   |   |-- ClaimsService.API.csproj
    |   |   |-- ClaimsService.API.http
    |   |   |-- appsettings.Development.json
    |   |   |-- appsettings.json
    |   |   |-- Controllers/
    |   |   |   `-- ClaimsController.cs
    |   |   |-- Dockerfile
    |   |   |-- Dockerfile.dockerignore
    |   |   |-- Program.cs
    |   |   `-- Properties/
    |   |-- ClaimsService.Application/
    |   |   |-- ClaimsService.Application.csproj
    |   |   |-- Commands/
    |   |   |   `-- SubmitClaimCommand.cs
    |   |   |-- Handlers/
    |   |   |   `-- SubmitClaimCommandHandler.cs
    |   |   |-- Interfaces/
    |   |   |   |-- IClaimRepository.cs
    |   |   |   |-- IEventPublisher.cs
    |   |   |   `-- IUnitOfWork.cs
    |   |   `-- Sagas/
    |   |       |-- ClaimProcessingSagaDefinition.cs
    |   |       |-- ClaimProcessingSagaState.cs
    |   |       `-- ClaimProcessingSagaStateMachine.cs
    |   |-- ClaimsService.Domain/
    |   |   |-- ClaimsService.Domain.csproj
    |   |   |-- Entities/
    |   |   |   `-- Claim.cs
    |   |   `-- Enums/
    |   |       `-- ClaimStatus.cs
    |   `-- ClaimsService.Infrastructure/
    |       |-- ClaimsService.Infrastructure.csproj
    |       |-- Messaging/
    |       |   `-- EventPublisher.cs
    |       |-- Persistance/
    |       |   |-- ClaimsDbContext.cs
    |       |   |-- Configurations/
    |       |   |   `-- ClaimConfiguration.cs
    |       |   |-- EfUnitOfWork.cs
    |       |   `-- Migrations/
    |       |       |-- 20260308083143_InitialFullSchema.cs
    |       |       `-- ClaimsDbContextModelSnapshot.cs
    |       `-- Repositories/
    |           `-- ClaimRepository.cs
    |-- document-service/
    |   |-- DocumentService.slnx
    |   |-- DocumentService.API/
    |   |   |-- appsettings.Development.json
    |   |   |-- appsettings.json
    |   |   |-- DocumentService.API.csproj
    |   |   |-- DocumentService.API.http
    |   |   |-- Dockerfile
    |   |   |-- Dockerfile.dockerignore
    |   |   |-- Program.cs
    |   |   `-- Properties/
    |   |-- DocumentService.Application/
    |   |   |-- DocumentService.Application.csproj
    |   |   |-- Commands/
    |   |   |-- DTOs/
    |   |   |-- Interfaces/
    |   |   `-- Queries/
    |   |-- DocumentService.Domain/
    |   |   |-- DocumentService.Domain.csproj
    |   |   |-- Entities/
    |   |   |-- Events/
    |   |   `-- ValueObjects/
    |   `-- DocumentService.Infrastructure/
    |       |-- DocumentService.Infrastructure.csproj
    |       |-- Messaging/
    |       |-- Persistence/
    |       |-- Repository/
    |       `-- Storage/
    |-- fraud-service/
    `-- notification-service/
        |-- NotificationService.slnx
        |-- NotificationService.API/
        |   |-- NotificationService.API.csproj
        |   |-- NotificationService.API.http
        |   |-- appsettings.Development.json
        |   |-- appsettings.json
        |   |-- Dockerfile
        |   |   |-- Dockerfile.dockerignore
        |   |-- logs/
        |   |-- Program.cs
        |   `-- Properties/
        |-- NotificationService.Application/
        |   |-- NotificationService.Application.csproj
        |   |-- Commands/
        |   |   `-- CreateNotification/
        |   |       |-- CreateNotificationCommand.cs
        |   |       `-- CreateNotificationCommandHandler.cs
        |   `-- Interfaces/
        |       |-- INotificationRepository.cs
        |       `-- IUnitOfWork.cs
        |-- NotificationService.Domain/
        |   |-- NotificationService.Domain.csproj
        |   |-- Entities/
        |   |   `-- Notification.cs
        |   `-- Enums/
        |       |-- NotificationChannel.cs
        |       `-- NotificationStatus.cs
        `-- NotificationService.Infrastructure/
            |-- NotificationService.Infrastructure.csproj
            |-- Messaging/
            |   `-- Consumers/
            |       `-- ClaimSubmittedConsumer.cs
            |-- Persistence/
            |   |-- NotificationDbContext.cs
            |   |-- Configurations/
            |   |   `-- NotificationConfiguration.cs
            |   |-- EfUnitOfWork.cs
            |   |-- Migrations/
            |   |   |-- 20260311132901_InitialCreate.cs
            |   |   `-- NotificationDbContextModelSnapshot.cs
            |   `-- Repositories/
            |       `-- NotificationRepository.cs
            |-- Senders/
            |   |-- EmailSender.cs
            |   `-- INotificationSender.cs
            `-- Workers/
                |-- NotificationDispatcher.cs
                `-- NotificationDispatcherOptions.cs
```