using MassTransit;
using BuildingBlocks.Contracts.Claims;
using BuildingBlocks.Contracts.Documents;
using BuildingBlocks.Contracts.Fraud;
using BuildingBlocks.Contracts.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimsService.Application.Sagas;

public class ClaimProcessingSagaStateMachine :
    MassTransitStateMachine<ClaimProcessingSagaState>
{
    private readonly ILogger<ClaimProcessingSagaStateMachine> _logger;
    private readonly Uri _claimsServiceQueueUri;
    private readonly Uri _notificationServiceQueueUri;
    private readonly Uri _fraudServiceQueueUri;
    private readonly Uri _paymentServiceQueueUri;

    public State WaitingForDocuments { get; private set; } = null!;
    public State FraudCheckRunning { get; private set; } = null!;
    public State Rejected { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;
    //public State PaymentFailed { get; private set; } = null!;

    public Event<ClaimSubmitted> ClaimSubmitted { get; private set; } = null!;
    public Event<DocumentUploaded> DocumentUploaded { get; private set; } = null!;
    public Event<FraudCheckCompleted> FraudCheckCompleted { get; private set; } = null!;
    public Event<PaymentProcessed> PaymentProcessed { get; private set; } = null!;

    /// <summary>
    /// Initializes the ClaimProcessingSagaStateMachine that orchestrates the claim processing workflow.
    /// 
    /// The saga manages the following states and transitions:
    /// 1. Initial: Waits for ClaimSubmitted event, stores claim data, requests required documents
    /// 2. WaitingForDocuments: Collects uploaded documents until all required documents are received, then initiates fraud check
    /// 3. FraudCheckRunning: Processes fraud check results - rejects if fraudulent, otherwise proceeds to payment
    /// 4. PaymentProcessing: Handles payment processing - rejects if payment fails, finalizes if successful
    /// 
    /// Event Correlations:
    /// - ClaimSubmitted: Correlates and selects saga instance by ClaimId
    /// - DocumentUploaded: Correlates saga instance by ClaimId
    /// - FraudCheckCompleted: Correlates saga instance by ClaimId
    /// - PaymentProcessed: Correlates saga instance by ClaimId
    /// 
    /// Note regarding Finalize(): When calling Finalize() in the saga state machine,
    /// you do NOT need to explicitly add a TransitionTo() before it. Finalize() implicitly
    /// transitions the saga to the completed state and terminates the saga instance.
    /// The SetCompletedWhenFinalized() configuration handles this automatically.
    /// </summary>
    public ClaimProcessingSagaStateMachine(
        IOptions<ClaimProcessingSagaRoutingOptions> routingOptions,
        ILogger<ClaimProcessingSagaStateMachine> logger)
    {
        _logger = logger;
        var routes = routingOptions.Value;
        _claimsServiceQueueUri = new Uri($"queue:{routes.ClaimsServiceQueue}");
        _notificationServiceQueueUri = new Uri($"queue:{routes.NotificationServiceQueue}");
        _fraudServiceQueueUri = new Uri($"queue:{routes.FraudServiceQueue}");
        _paymentServiceQueueUri = new Uri($"queue:{routes.PaymentServiceQueue}");

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

                    _logger.LogInformation(
                        "Saga started. SagaId={SagaId}, ClaimId={ClaimId}, Amount={Amount}, RequiredDocuments={RequiredDocuments}",
                        context.Saga.CorrelationId,
                        context.Saga.ClaimId,
                        context.Saga.ClaimAmount,
                        string.Join(", ", context.Saga.RequiredDocuments));
                })                                  
                .Send(_notificationServiceQueueUri, context =>
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

                    // _logger.LogInformation(
                    //     "Document received. SagaId={SagaId}, ClaimId={ClaimId}, DocumentType={DocumentType}, Progress={Uploaded}/{Required}",
                    //     context.Saga.CorrelationId,
                    //     context.Saga.ClaimId,
                    //     context.Message.DocumentType,
                    //     context.Saga.UploadedDocuments.Count,
                    //     context.Saga.RequiredDocuments.Count);
                })

                .If(context =>
                        context.Saga.RequiredDocuments.All(doc =>
                            context.Saga.UploadedDocuments.Contains(doc)),
                    binder => binder
                        .Then(context => _logger.LogInformation(
                            "All documents received, initiating fraud check. SagaId={SagaId}, ClaimId={ClaimId}",
                            context.Saga.CorrelationId,
                            context.Saga.ClaimId))
                        .Send(_claimsServiceQueueUri, context =>
                            new MarkClaimUnderReview(context.Saga.ClaimId))
                        .Send(_fraudServiceQueueUri, context =>
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

                    _logger.LogInformation(
                        "Fraud check result received. SagaId={SagaId}, ClaimId={ClaimId}, RiskScore={RiskScore}, IsFraudulent={IsFraudulent}",
                        context.Saga.CorrelationId,
                        context.Saga.ClaimId,
                        context.Message.RiskScore,
                        context.Message.IsFraudulent);
                })
                .IfElse(context => context.Message.RiskScore >= 0.8m
                                || context.Message.IsFraudulent,
                    thenBinder => thenBinder
                        .Then(context => _logger.LogWarning(
                            "Claim rejected due to fraud. SagaId={SagaId}, ClaimId={ClaimId}, RiskScore={RiskScore}, Reason={Reason}",
                            context.Saga.CorrelationId,
                            context.Saga.ClaimId,
                            context.Saga.FraudRiskScore,
                            context.Saga.FraudReason))
                        .Send(_claimsServiceQueueUri,
                                    ctx => new MarkClaimRejected(
                                        ctx.Saga.ClaimId,
                                        ctx.Message.Reason))
                        .Finalize(),
                    elseBinder => elseBinder
                        .Then(context => _logger.LogInformation(
                            "Fraud check passed, initiating payment. SagaId={SagaId}, ClaimId={ClaimId}, Amount={Amount}",
                            context.Saga.CorrelationId,
                            context.Saga.ClaimId,
                            context.Saga.ClaimAmount))
                        .Send(_paymentServiceQueueUri,
                            context => new ProcessPayment(context.Saga.ClaimId, context.Saga.ClaimAmount))
                        .TransitionTo(PaymentProcessing)
                )
        );

        During(PaymentProcessing,
            When(PaymentProcessed)
                .IfElse(context => !context.Message.Success,
                    thenBinder => thenBinder
                        .Then(context => _logger.LogWarning(
                            "Claim rejected due to payment failure. SagaId={SagaId}, ClaimId={ClaimId}",
                            context.Saga.CorrelationId,
                            context.Saga.ClaimId))
                        .Send(_claimsServiceQueueUri,
                                    ctx => new MarkClaimRejected(
                                        ctx.Saga.ClaimId,
                                        "Payment processing failed"))
                        //.TransitionTo(Rejected) // In a real system, you might want a separate "PaymentFailed" state to allow for retries
                        .Finalize(),
                    elseBinder => elseBinder
                        .Then(context => _logger.LogInformation(
                            "Claim approved, payment processed successfully. SagaId={SagaId}, ClaimId={ClaimId}",
                            context.Saga.CorrelationId,
                            context.Saga.ClaimId))
                        .Send(_claimsServiceQueueUri,
                                    ctx => new MarkClaimApproved(ctx.Saga.ClaimId))
                        .Finalize()
                )
        );

    }
}