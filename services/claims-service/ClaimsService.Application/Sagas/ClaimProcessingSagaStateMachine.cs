using MassTransit;
using BuildingBlocks.Contracts.Claims;
using BuildingBlocks.Contracts.Documents;
using BuildingBlocks.Contracts.Fraud;
using BuildingBlocks.Contracts.Payment;

namespace ClaimsService.Application.Sagas;

public class ClaimProcessingSagaStateMachine :
    MassTransitStateMachine<ClaimProcessingSagaState>
{
    public State WaitingForDocuments { get; private set; } = null!;
    public State FraudCheckRunning { get; private set; } = null!;
    public State Rejected { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;
    //public State PaymentFailed { get; private set; } = null!;

    public Event<ClaimSubmitted> ClaimSubmitted { get; private set; } = null!;
    public Event<DocumentUploaded> DocumentUploaded { get; private set; } = null!;
    public Event<FraudCheckCompleted> FraudCheckCompleted { get; private set; } = null!;
    public Event<PaymentProcessed> PaymentProcessed { get; private set; } = null!;

    public ClaimProcessingSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        SetCompletedWhenFinalized();

        Event(() => ClaimSubmitted, x =>
        {
            x.CorrelateById(context => context.Message.ClaimId);
            x.SelectId(context => context.Message.ClaimId);
        });

        Event(() => DocumentUploaded, x =>
        {
            x.CorrelateById(context => context.Message.ClaimId);
        });

        Event(() => FraudCheckCompleted, x =>
        {
            x.CorrelateById(context => context.Message.ClaimId);
        });

        Event(() => PaymentProcessed, x =>
        {
            x.CorrelateById(context => context.Message.ClaimId);
        });

        Initially(
            When(ClaimSubmitted)
                .Then(context =>
                {
                    context.Saga.ClaimId = context.Message.ClaimId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.ClaimAmount = context.Message.ClaimAmount;

                    context.Saga.RequiredDocuments =
                        context.Message.RequiredDocuments.ToList();
                })
                .Send(new Uri("queue:notification-service"), context =>
                    new RequestDocuments(
                        context.Message.ClaimId,
                        context.Message.CustomerId,
                        context.Message.RequiredDocuments))
                .TransitionTo(WaitingForDocuments)
        );

        During(WaitingForDocuments,

            When(DocumentUploaded)
                .Then(context =>
                {
                    // Prevent duplicate processing
                    if (!context.Saga.UploadedDocuments
                        .Contains(context.Message.DocumentType))
                    {
                        context.Saga.UploadedDocuments
                            .Add(context.Message.DocumentType);
                    }
                })

                .If(context =>
                        context.Saga.RequiredDocuments.All(doc =>
                            context.Saga.UploadedDocuments.Contains(doc)),
                    binder => binder
                        .Send(new Uri("queue:fraud-service"), context =>
                            new RunFraudCheck(context.Saga.ClaimId))
                        .TransitionTo(FraudCheckRunning)
                )
        );

        During(FraudCheckRunning,
            When(FraudCheckCompleted)
                .Then(context =>
                {
                    context.Saga.FraudRiskScore = context.Message.RiskScore;
                    context.Saga.IsFraudulent = context.Message.IsFraudulent;
                    context.Saga.FraudReason = context.Message.Reason;
                    context.Saga.FraudEvaluatedAt = context.Message.EvaluatedAt;
                })
                .IfElse(context => context.Message.RiskScore >= 0.8m
                                || context.Message.IsFraudulent,
                    thenBinder => thenBinder.TransitionTo(Rejected),
                    elseBinder => elseBinder
                        .Send(new Uri("queue:payment-service"),
                            context => new ProcessPayment(context.Saga.ClaimId, context.Saga.ClaimAmount))
                        .TransitionTo(PaymentProcessing)
                )
        );

        During(PaymentProcessing,
            When(PaymentProcessed)
                .IfElse(context => !context.Message.Success,
                    thenBinder => thenBinder
                        .TransitionTo(Rejected) // In a real system, you might want a separate "PaymentFailed" state to allow for retries
                        .Finalize(),
                    elseBinder => elseBinder
                        .Finalize()
                )
        );

    }
}