using BuildingBlocks.Contracts.Payment;

namespace PaymentService.Application;

public interface IPaymentProcessor
{
    Task<PaymentProcessed> ProcessAsync(ProcessPayment command, CancellationToken cancellationToken);
}
