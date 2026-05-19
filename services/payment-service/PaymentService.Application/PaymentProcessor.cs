using BuildingBlocks.Contracts.Payment;
using Microsoft.Extensions.Logging;

namespace PaymentService.Application;

public sealed class PaymentProcessor(ILogger<PaymentProcessor> logger) : IPaymentProcessor
{
    public async Task<PaymentProcessed> ProcessAsync(ProcessPayment command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Payment processing started. ClaimId={ClaimId}, Amount={Amount}",
            command.ClaimId,
            command.Amount);

        await Task.Delay(1500, cancellationToken);

        var transactionRef = Guid.NewGuid().ToString();

        logger.LogInformation(
            "Payment processing completed. ClaimId={ClaimId}, Success={Success}, TransactionRef={TransactionRef}",
            command.ClaimId,
            true,
            transactionRef);

        return new PaymentProcessed(
            command.ClaimId,
            true,
            null,
            transactionRef,
            DateTime.UtcNow);
    }
}
