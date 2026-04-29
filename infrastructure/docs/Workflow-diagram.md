# Workflow Diagram

Generated: 2026-04-28

## Primary Workflow (Azure Service Bus)

```mermaid
flowchart TD
    A[Client] --> B[Claims API]
    B --> C[SubmitClaim Command]
    C --> D[Claims Domain + DB]
    D --> E[MassTransit Outbox]
    E --> F[Azure Service Bus]

    F --> G[ClaimProcessingSaga]

    G --> H[RequestDocuments]
    H --> I[Notification Service]

    G --> J[RunFraudCheck]
    J --> K[Fraud Service]
    K --> L[FraudCheckCompleted]
    L --> G

    G --> M[ProcessPayment]
    M --> N[Payment Service]
    N --> O[PaymentCompleted]
    O --> G

    G --> P[Claim Completed]
```

## Document Side Path (Mode-Dependent)

```mermaid
flowchart TD
    A[Client] --> B[Document API: Generate Upload URL]
    B --> C{Storage Mode}

    C -->|Azure Blob configured| D[AzureBlobObjectStorage]
    C -->|No Blob connection string| E[MinIO mode]

    E --> F[ObjectCreatedConsumer]
    F --> G[Document + Outbox rows]
    G --> H[OutboxDispatcher + RabbitPublisher]
    H --> I[Raw DocumentUploaded message]
    I --> J[Claims DocumentUploadedBridgeConsumer]
    J --> K[Typed DocumentUploaded event]
    K --> L[ClaimProcessingSaga]
```

## Observability Overlay Effect

- Base runtime: no OTLP endpoint injected.
- Observability overlay: sets `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317` and enables Seq/Jaeger/Prometheus/Grafana.