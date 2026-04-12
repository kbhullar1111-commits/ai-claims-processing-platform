using MassTransit;
using BuildingBlocks.Contracts.Payment;

public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        await Task.Delay(1500);

        await context.Publish(new PaymentProcessed(
            context.Message.ClaimId,
            true,
            null,
            Guid.NewGuid().ToString(),
            DateTime.UtcNow));
    }
}