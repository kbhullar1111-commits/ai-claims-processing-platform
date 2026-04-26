using MassTransit;
using BuildingBlocks.Contracts.Payment;
using Microsoft.Extensions.Logging;

public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    private readonly ILogger<ProcessPaymentConsumer> _logger;

    public ProcessPaymentConsumer(ILogger<ProcessPaymentConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        _logger.LogInformation(
            "Payment processing started. ClaimId={ClaimId}, Amount={Amount}",
            context.Message.ClaimId,
            context.Message.Amount);

        await Task.Delay(1500);

        var success = true;
        var transactionRef = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Payment processing completed. ClaimId={ClaimId}, Success={Success}, TransactionRef={TransactionRef}",
            context.Message.ClaimId,
            success,
            transactionRef);

        await context.Publish(new PaymentProcessed(
            context.Message.ClaimId,
            success,
            null,
            transactionRef,
            DateTime.UtcNow));
    }
}