# Architecture Review

Generated: 2026-03-13

## Purpose

This document captures the current architectural shape of the solution so it can evolve with the system. It explains how the main layers are used, how requests and events flow, and why files belong in their current sections.

## Solution Shape

The workspace is organized as a set of services plus shared building blocks:

- `building-blocks/contracts`: shared integration contracts exchanged between services.
- `services/claims-service`: claim submission and claim lifecycle entry point.
- `services/notification-service`: event-driven notification persistence and dispatch.
- `infrastructure`: environment-level assets such as docker and architecture documentation.

Each implemented service follows a consistent four-part structure:

- `API`: transport and composition root.
- `Application`: use cases, orchestration, and interfaces.
- `Domain`: business entities and rules.
- `Infrastructure`: persistence, messaging, external integrations, and workers.

This is a pragmatic Clean Architecture style. Dependencies point inward toward `Application` and `Domain`, while implementations of external concerns stay in `Infrastructure`.

## Layer Responsibilities

### API Layer

The API layer owns entry points and application startup.

In the claims service, [services/claims-service/ClaimsService.API/Program.cs](services/claims-service/ClaimsService.API/Program.cs) configures:

- ASP.NET Core controllers
- EF Core with PostgreSQL
- MediatR registration
- MassTransit with RabbitMQ
- MassTransit Entity Framework outbox
- dependency injection bindings for repository, unit of work, and event publisher

[services/claims-service/ClaimsService.API/Controllers/ClaimsController.cs](services/claims-service/ClaimsService.API/Controllers/ClaimsController.cs) is intentionally thin. It accepts the HTTP request and sends a command through MediatR. It does not implement business logic.

In the notification service, [services/notification-service/NotificationService.API/Program.cs](services/notification-service/NotificationService.API/Program.cs) configures:

- Serilog
- EF Core with PostgreSQL
- MassTransit consumer registration
- MediatR registration
- repository and unit of work bindings
- notification senders
- hosted background worker registration

Architectural rule: the API layer should know frameworks and wiring, but it should not own use-case logic.

### Application Layer

The application layer models use cases.

Examples in claims service:

- [services/claims-service/ClaimsService.Application/Commands/SubmitClaimCommand.cs](services/claims-service/ClaimsService.Application/Commands/SubmitClaimCommand.cs)
- [services/claims-service/ClaimsService.Application/Handlers/SubmitClaimCommandHandler.cs](services/claims-service/ClaimsService.Application/Handlers/SubmitClaimCommandHandler.cs)
- [services/claims-service/ClaimsService.Application/Interfaces/IClaimRepository.cs](services/claims-service/ClaimsService.Application/Interfaces/IClaimRepository.cs)
- [services/claims-service/ClaimsService.Application/Interfaces/IEventPublisher.cs](services/claims-service/ClaimsService.Application/Interfaces/IEventPublisher.cs)
- [services/claims-service/ClaimsService.Application/Interfaces/IUnitOfWork.cs](services/claims-service/ClaimsService.Application/Interfaces/IUnitOfWork.cs)

Examples in notification service:

- [services/notification-service/NotificationService.Application/Commands/CreateNotification/CreateNotificationCommand.cs](services/notification-service/NotificationService.Application/Commands/CreateNotification/CreateNotificationCommand.cs)
- [services/notification-service/NotificationService.Application/Commands/CreateNotification/CreateNotificationCommandHandler.cs](services/notification-service/NotificationService.Application/Commands/CreateNotification/CreateNotificationCommandHandler.cs)
- [services/notification-service/NotificationService.Application/Interfaces/INotificationRepository.cs](services/notification-service/NotificationService.Application/Interfaces/INotificationRepository.cs)
- [services/notification-service/NotificationService.Application/Interfaces/IUnitOfWork.cs](services/notification-service/NotificationService.Application/Interfaces/IUnitOfWork.cs)

Why handlers live in `Application`:

- a handler represents a use case
- it coordinates domain behavior and external dependencies through interfaces
- it should not know HTTP, RabbitMQ, EF Core, or other transport details directly

The application layer depends on abstractions, not implementations.

### Domain Layer

The domain layer owns business rules and state transitions.

Claims service domain example:

- [services/claims-service/ClaimsService.Domain/Entities/Claim.cs](services/claims-service/ClaimsService.Domain/Entities/Claim.cs)

This entity enforces invariants such as:

- claim amount must be greater than zero
- only submitted claims can move to review
- only approved claims can be paid
- only paid or rejected claims can be closed

Notification service domain example:

- [services/notification-service/NotificationService.Domain/Entities/Notification.cs](services/notification-service/NotificationService.Domain/Entities/Notification.cs)

This entity owns notification lifecycle rules such as:

- initial pending state
- retry scheduling
- sent and failed transitions

Architectural rule: domain code should stay free of framework-specific dependencies whenever possible.

### Infrastructure Layer

Infrastructure implements the interfaces defined by the application layer and adapts external systems.

Claims service infrastructure examples:

- [services/claims-service/ClaimsService.Infrastructure/Repositories/ClaimRepository.cs](services/claims-service/ClaimsService.Infrastructure/Repositories/ClaimRepository.cs)
- [services/claims-service/ClaimsService.Infrastructure/Persistance/ClaimsDbContext.cs](services/claims-service/ClaimsService.Infrastructure/Persistance/ClaimsDbContext.cs)
- [services/claims-service/ClaimsService.Infrastructure/Persistance/EfUnitOfWork.cs](services/claims-service/ClaimsService.Infrastructure/Persistance/EfUnitOfWork.cs)
- [services/claims-service/ClaimsService.Infrastructure/Messaging/EventPublisher.cs](services/claims-service/ClaimsService.Infrastructure/Messaging/EventPublisher.cs)

Notification service infrastructure examples:

- [services/notification-service/NotificationService.Infrastructure/Persistence/Repositories/NotificationRepository.cs](services/notification-service/NotificationService.Infrastructure/Persistence/Repositories/NotificationRepository.cs)
- [services/notification-service/NotificationService.Infrastructure/Persistence/NotificationDbContext.cs](services/notification-service/NotificationService.Infrastructure/Persistence/NotificationDbContext.cs)
- [services/notification-service/NotificationService.Infrastructure/Persistence/EfUnitOfWork.cs](services/notification-service/NotificationService.Infrastructure/Persistence/EfUnitOfWork.cs)
- [services/notification-service/NotificationService.Infrastructure/Messaging/Consumers/ClaimSubmittedConsumer.cs](services/notification-service/NotificationService.Infrastructure/Messaging/Consumers/ClaimSubmittedConsumer.cs)
- [services/notification-service/NotificationService.Infrastructure/Workers/NotificationDispatcher.cs](services/notification-service/NotificationService.Infrastructure/Workers/NotificationDispatcher.cs)
- [services/notification-service/NotificationService.Infrastructure/Senders/EmailSender.cs](services/notification-service/NotificationService.Infrastructure/Senders/EmailSender.cs)

Why these files belong in `Infrastructure`:

- repositories depend on EF Core and database access details
- consumers depend on MassTransit and broker integration
- senders depend on external delivery mechanisms
- workers depend on hosting/runtime mechanics

## End-to-End Request and Event Flow

### Claims Submission Flow

1. HTTP request enters [services/claims-service/ClaimsService.API/Controllers/ClaimsController.cs](services/claims-service/ClaimsService.API/Controllers/ClaimsController.cs).
2. The controller sends `SubmitClaimCommand` through MediatR.
3. MediatR resolves [services/claims-service/ClaimsService.Application/Handlers/SubmitClaimCommandHandler.cs](services/claims-service/ClaimsService.Application/Handlers/SubmitClaimCommandHandler.cs).
4. The handler creates a domain `Claim` via [services/claims-service/ClaimsService.Domain/Entities/Claim.cs](services/claims-service/ClaimsService.Domain/Entities/Claim.cs).
5. The handler stages the entity via `IClaimRepository`.
6. The handler publishes the shared contract `ClaimSubmitted` from [building-blocks/contracts/BuildingBlocks.Contracts/Claims/ClaimSubmitted.cs](building-blocks/contracts/BuildingBlocks.Contracts/Claims/ClaimSubmitted.cs).
7. The handler commits through `IUnitOfWork`.

This keeps the controller thin and makes the handler the application-level coordinator.

### Notification Processing Flow

1. MassTransit receives `ClaimSubmitted` through RabbitMQ.
2. [services/notification-service/NotificationService.Infrastructure/Messaging/Consumers/ClaimSubmittedConsumer.cs](services/notification-service/NotificationService.Infrastructure/Messaging/Consumers/ClaimSubmittedConsumer.cs) consumes the message.
3. The consumer maps the message to `CreateNotificationCommand` and sends it via MediatR.
4. [services/notification-service/NotificationService.Application/Commands/CreateNotification/CreateNotificationCommandHandler.cs](services/notification-service/NotificationService.Application/Commands/CreateNotification/CreateNotificationCommandHandler.cs) checks for duplicates, creates a domain `Notification`, persists it, and commits.
5. [services/notification-service/NotificationService.Infrastructure/Workers/NotificationDispatcher.cs](services/notification-service/NotificationService.Infrastructure/Workers/NotificationDispatcher.cs) polls pending notifications.
6. The dispatcher selects the appropriate sender, currently [services/notification-service/NotificationService.Infrastructure/Senders/EmailSender.cs](services/notification-service/NotificationService.Infrastructure/Senders/EmailSender.cs).
7. The notification is marked sent, retried later, or marked failed.

This split is architecturally useful because event consumption and actual notification delivery are decoupled.

## MediatR In This Solution

MediatR is used for in-process use-case dispatch.

Claims service registration is in [services/claims-service/ClaimsService.API/Program.cs](services/claims-service/ClaimsService.API/Program.cs).
Notification service registration is in [services/notification-service/NotificationService.API/Program.cs](services/notification-service/NotificationService.API/Program.cs).

How it works here:

- controllers send commands to handlers
- consumers also send commands to handlers
- handlers encapsulate one use case each

Architectural value:

- consistent use-case entry model regardless of transport
- thinner controllers and consumers
- improved testability because business orchestration is centralized

In short:

- MediatR handles in-process flow
- MassTransit handles inter-service flow

## MassTransit In This Solution

MassTransit is used for asynchronous service-to-service integration over RabbitMQ.

Claims service producer side:

- configuration in [services/claims-service/ClaimsService.API/Program.cs](services/claims-service/ClaimsService.API/Program.cs)
- publish abstraction implemented in [services/claims-service/ClaimsService.Infrastructure/Messaging/EventPublisher.cs](services/claims-service/ClaimsService.Infrastructure/Messaging/EventPublisher.cs)

Notification service consumer side:

- configuration in [services/notification-service/NotificationService.API/Program.cs](services/notification-service/NotificationService.API/Program.cs)
- consumer in [services/notification-service/NotificationService.Infrastructure/Messaging/Consumers/ClaimSubmittedConsumer.cs](services/notification-service/NotificationService.Infrastructure/Messaging/Consumers/ClaimSubmittedConsumer.cs)

Important architectural point:

The claims service configures the MassTransit Entity Framework outbox in [services/claims-service/ClaimsService.API/Program.cs](services/claims-service/ClaimsService.API/Program.cs), and the outbox entities are added in [services/claims-service/ClaimsService.Infrastructure/Persistance/ClaimsDbContext.cs](services/claims-service/ClaimsService.Infrastructure/Persistance/ClaimsDbContext.cs). This reduces the risk of saving a claim successfully while losing the outgoing event.

The notification service then consumes the event and converts it back into an application command. That adapter pattern is one of the cleanest design choices in the current solution.

## Repository Pattern In This Solution

The repository pattern is implemented as a thin abstraction over EF Core.

Claims service:

- interface in [services/claims-service/ClaimsService.Application/Interfaces/IClaimRepository.cs](services/claims-service/ClaimsService.Application/Interfaces/IClaimRepository.cs)
- implementation in [services/claims-service/ClaimsService.Infrastructure/Repositories/ClaimRepository.cs](services/claims-service/ClaimsService.Infrastructure/Repositories/ClaimRepository.cs)
- commit boundary in [services/claims-service/ClaimsService.Infrastructure/Persistance/EfUnitOfWork.cs](services/claims-service/ClaimsService.Infrastructure/Persistance/EfUnitOfWork.cs)

Notification service:

- interface in [services/notification-service/NotificationService.Application/Interfaces/INotificationRepository.cs](services/notification-service/NotificationService.Application/Interfaces/INotificationRepository.cs)
- implementation in [services/notification-service/NotificationService.Infrastructure/Persistence/Repositories/NotificationRepository.cs](services/notification-service/NotificationService.Infrastructure/Persistence/Repositories/NotificationRepository.cs)
- commit boundary in [services/notification-service/NotificationService.Infrastructure/Persistence/EfUnitOfWork.cs](services/notification-service/NotificationService.Infrastructure/Persistence/EfUnitOfWork.cs)

How it behaves:

- repositories stage adds and queries against `DbContext`
- the unit of work performs `SaveChangesAsync`
- handlers coordinate repository calls and commit when the use case is complete

This is a lightweight repository pattern. It keeps EF Core outside the application layer without introducing unnecessary complexity.

## Shared Contracts And Building Blocks

[building-blocks/contracts/BuildingBlocks.Contracts/Claims/ClaimSubmitted.cs](building-blocks/contracts/BuildingBlocks.Contracts/Claims/ClaimSubmitted.cs) is the integration contract published by claims service and consumed by notification service.

Why this contract belongs in shared building blocks:

- producer and consumer share a stable message schema
- services do not reference each other directly
- integration remains explicit and versionable

This is a standard microservice integration practice and a good architectural choice.

## Current Strengths

- clear layer separation
- thin controllers and consumers
- domain entities enforce core state transitions
- MediatR standardizes application use-case entry points
- MassTransit decouples services asynchronously
- claims service uses outbox support for more reliable event publication
- notification service separates persistence from delivery through a background dispatcher

## Current Gaps And Risks To Watch

- the repository pattern is intentionally thin; if use cases grow, query complexity may start to leak into handlers unless query patterns are formalized
- the claims service uses an application-orchestrated event publishing style rather than domain events; this is fine now, but richer domain workflows may push toward aggregate-raised domain events later
- naming is mostly consistent, but `Persistance` in claims service is misspelled; it is harmless technically but worth correcting before the codebase grows further
- notification dispatch currently logs email sending rather than integrating a real provider; that is expected, but production delivery concerns are still open
- notification service currently relies on polling for dispatch; this is simple and robust, but throughput and latency expectations should be revisited as volume grows

## Architectural Summary

The current solution is a solid early-stage architecture for an event-driven modular system:

- HTTP enters through the API layer
- application handlers coordinate use cases through MediatR
- domain entities protect business rules
- infrastructure implements storage and messaging concerns
- MassTransit connects services asynchronously using shared contracts
- repositories and unit of work abstract EF Core from the application layer

The design is pragmatic, understandable, and suitable for incremental growth if the current boundaries are maintained.

## Suggested Maintenance Approach

Keep this document updated whenever one of the following changes occurs:

- a new service is added
- a new cross-service event is introduced
- a new background worker or integration adapter is added
- a layer boundary changes
- a reliability pattern such as inbox, outbox, saga, or retries is added or changed