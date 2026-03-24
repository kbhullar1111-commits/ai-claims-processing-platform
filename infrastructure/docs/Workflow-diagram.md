                ┌─────────────────────────┐
                │        Client           │
                └───────────┬─────────────┘
                            │
                            ▼
                ┌─────────────────────────┐
                │      ClaimsService      │
                │  Claim Aggregate       │
                │  Outbox Pattern        │
                └───────────┬─────────────┘
                            │
                            ▼
                     ClaimSubmitted
                            │
                            ▼
                    RabbitMQ Message Bus
                            │
                            ▼
                ┌─────────────────────────┐
                │   ClaimProcessingSaga   │
                │  (State Machine)        │
                └───────┬───────┬─────────┘
                        │       │
                        │       │
                        ▼       ▼
            RequestDocuments   Notifications
                        │
                        ▼
                ┌────────────────────┐
                │  DocumentService   │
                └─────────┬──────────┘
                          │
                          ▼
                   DocumentsUploaded
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