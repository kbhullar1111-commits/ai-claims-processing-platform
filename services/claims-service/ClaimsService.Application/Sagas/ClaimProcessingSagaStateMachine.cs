using MassTransit;
using BuildingBlocks.Contracts.Claims;
using BuildingBlocks.Contracts.Documents;
using ClaimsService.Application.Commands;

namespace ClaimsService.Application.Sagas;

public class ClaimProcessingSagaStateMachine :
    MassTransitStateMachine<ClaimProcessingSagaState>
{
    public State WaitingForDocuments { get; private set; } = null!;
    public State FraudCheckRunning { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;

    public Event<ClaimSubmitted> ClaimSubmitted { get; private set; } = null!;
    public Event<DocumentUploaded> DocumentUploaded { get; private set; } = null!;

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
                .Send(new Uri("queue:document-service"), context =>
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
    }
}