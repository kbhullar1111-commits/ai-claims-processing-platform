using MassTransit;

namespace ClaimsService.Application.Sagas;

public class ClaimProcessingSagaState :
    SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = default!;

    public Guid ClaimId { get; set; }

    public Guid CustomerId { get; set; }

    public decimal ClaimAmount { get; set; }

    public List<string> RequiredDocuments { get; set; } = new();

    public List<string> UploadedDocuments { get; set; } = new();

    public DateTime? DocumentsDeadline { get; set; }

    public int RetryCount { get; set; }
}