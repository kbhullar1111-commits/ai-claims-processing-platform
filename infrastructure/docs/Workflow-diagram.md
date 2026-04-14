                ┌─────────────────────────┐
                │         Client          │
                └───────────┬─────────────┘
                            │
                            ▼
                ┌─────────────────────────┐
                │      ClaimsService      │
                │   Claim Aggregate       │
                │ MassTransit Outbox      │
                └───────────┬─────────────┘
                            │
                            ▼
                      ClaimSubmitted
                            │
                            ▼
                      RabbitMQ Bus
                            │
                            ▼
                ┌─────────────────────────┐
                │   ClaimProcessingSaga   │
                │   (State Machine)       │
                └───────┬────────┬────────┘
                        │        │
                        │        ▼
                        │   RequestDocuments
                        │        │
                        │        ▼
                        │  ┌────────────────────┐
                        │  │  DocumentService   │
                        │  │ Presigned Upload   │
                        │  └─────────┬──────────┘
                        │            │
                        │            ▼
                        │        MinIO Upload
                        │            │
                        │            ▼
                        │   Raw MinIO RabbitMQ Event
                        │            │
                        │            ▼
                        │  Custom ObjectCreatedConsumer
                        │            │
                        │            ▼
                        │  Document + OutboxMessage
                        │            │
                        │            ▼
                        │    OutboxDispatcher +
                        │     RabbitPublisher
                        │            │
                        │            ▼
                        │   Raw DocumentUploaded JSON
                        │            │
                        │            ▼
                        │  Claims Raw Bridge Consumer
                        │            │
                        │            ▼
                        └────► Typed DocumentUploaded
                                     │
                                     ▼
                            ClaimProcessingSaga
                                     │
                                     ▼
                               RunFraudCheck
                                     │
                                     ▼
                             ┌────────────────────┐
                             │    FraudService    │
                             └─────────┬──────────┘
                                       │
                                       ▼
                              FraudCheckCompleted
                                       │
                                       ▼
                               ClaimProcessingSaga
                                       │
                                       ▼
                                 ProcessPayment
                                       │
                                       ▼
                             ┌────────────────────┐
                             │   PaymentService   │
                             └─────────┬──────────┘
                                       │
                                       ▼
                                PaymentCompleted
                                       │
                                       ▼
                               ClaimProcessingSaga
                                       │
                                       ▼
                               Workflow Complete