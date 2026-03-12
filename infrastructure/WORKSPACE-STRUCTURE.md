# Workspace Folder Structure

Generated: 2026-03-11

```text
.
|-- building-blocks/
|   |-- contracts/
|   |   `-- BuildingBlocks.Contracts/
|   |       |-- Claims/
|   |       |-- Documents/
|   |       |-- Fraud/
|   |       `-- Payments/
|   `-- messaging/
|-- infrastructure/
|   `-- docker/
`-- services/
    |-- claims-service/
    |   |-- ClaimsService.API/
    |   |   |-- Controllers/
    |   |   `-- Properties/
    |   |-- ClaimsService.Application/
    |   |   |-- Commands/
    |   |   |-- Handlers/
    |   |   `-- Interfaces/
    |   |-- ClaimsService.Domain/
    |   |   |-- Entities/
    |   |   `-- Enums/
    |   `-- ClaimsService.Infrastructure/
    |       |-- Messaging/
    |       |-- Persistance/
    |       |   |-- Configurations/
    |       |   `-- Migrations/
    |       `-- Repositories/
    |-- document-service/
    |-- fraud-service/
    `-- notification-service/
        |-- NotificationService.API/
        |   `-- Properties/
        |-- NotificationService.Application/
        |   `-- Interfaces/
        |-- NotificationService.Domain/
        |   |-- Entities/
        |   `-- Enums/
        `-- NotificationService.Infrastructure/
            |-- Messaging/
            `-- Persistence/
                |-- Configurations/
                |-- MIgrations/
                `-- Repositories/
```
